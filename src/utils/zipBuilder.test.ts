import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { buildZipBytes, type ZipInputEntry } from './zipBuilder.ts';

test('buildZipBytes produces valid PKZIP archive readable by standard unzip tools', () => {
  const archiveJsonPath = path.resolve(process.cwd(), 'src/data/avaloniaArchive.json');
  const entries = JSON.parse(fs.readFileSync(archiveJsonPath, 'utf-8')) as ZipInputEntry[];
  assert.ok(entries.length >= 50, 'Expected full solution + DB entries in archive');

  const bytes = buildZipBytes(entries);
  assert.equal(bytes[0], 0x50); // 'P'
  assert.equal(bytes[1], 0x4b); // 'K'
  assert.equal(bytes[2], 0x03);
  assert.equal(bytes[3], 0x04);

  const tmpZip = path.resolve('/tmp', 'verify_client_zip_test.zip');
  fs.writeFileSync(tmpZip, bytes);

  const result = execFileSync('python3', [
    '-c',
    `import zipfile; z = zipfile.ZipFile(${JSON.stringify(tmpZip)}); bad = z.testzip(); print(bad if bad else "OK", len(z.namelist()))`,
  ], { encoding: 'utf-8' }).trim();

  fs.unlinkSync(tmpZip);
  assert.equal(result, `OK ${entries.length}`);
});
