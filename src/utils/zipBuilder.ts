export interface ZipInputEntry {
  path: string;
  content: string;
}

const CRC_TABLE = (() => {
  const table = new Uint32Array(256);
  for (let i = 0; i < 256; i++) {
    let c = i;
    for (let k = 0; k < 8; k++) {
      c = (c & 1) !== 0 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    }
    table[i] = c >>> 0;
  }
  return table;
})();

function crc32(bytes: Uint8Array): number {
  let crc = 0xffffffff;
  for (let i = 0; i < bytes.length; i++) {
    crc = CRC_TABLE[(crc ^ bytes[i]) & 0xff] ^ (crc >>> 8);
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function getDosDateTime(date = new Date()): { dosTime: number; dosDate: number } {
  const year = Math.max(1980, Math.min(2107, date.getFullYear()));
  const month = date.getMonth() + 1;
  const day = date.getDate();
  const hours = date.getHours();
  const minutes = date.getMinutes();
  const seconds = Math.floor(date.getSeconds() / 2);

  const dosTime = (hours << 11) | (minutes << 5) | seconds;
  const dosDate = ((year - 1980) << 9) | (month << 5) | day;
  return { dosTime, dosDate };
}

/**
 * Builds a standard PKZIP (STORE method 0, UTF-8 filenames) archive in memory.
 * Universally compatible with Windows Explorer, 7-Zip, WinRAR, macOS Archive Utility, and Linux unzip.
 */
export function buildZipBytes(entries: ZipInputEntry[]): Uint8Array {
  const encoder = new TextEncoder();
  const { dosTime, dosDate } = getDosDateTime();

  interface PreparedEntry {
    nameBytes: Uint8Array;
    dataBytes: Uint8Array;
    crc: number;
    offset: number;
  }

  const prepared: PreparedEntry[] = [];
  let localTotalSize = 0;
  let centralTotalSize = 0;

  for (const entry of entries) {
    const cleanPath = entry.path.replace(/^\/+/, '').replace(/\\/g, '/');
    const nameBytes = encoder.encode(cleanPath);
    const dataBytes = encoder.encode(entry.content);
    const crc = crc32(dataBytes);
    const offset = localTotalSize;

    prepared.push({ nameBytes, dataBytes, crc, offset });
    localTotalSize += 30 + nameBytes.length + dataBytes.length;
    centralTotalSize += 46 + nameBytes.length;
  }

  const totalSize = localTotalSize + centralTotalSize + 22;
  const out = new Uint8Array(totalSize);
  const view = new DataView(out.buffer);
  let cursor = 0;

  // Local file headers + payloads
  for (const item of prepared) {
    view.setUint32(cursor, 0x04034b50, true); // Local file header signature
    view.setUint16(cursor + 4, 20, true); // Version needed to extract (2.0)
    view.setUint16(cursor + 6, 0x0800, true); // UTF-8 filename flag
    view.setUint16(cursor + 8, 0, true); // Compression method: 0 (STORE)
    view.setUint16(cursor + 10, dosTime, true);
    view.setUint16(cursor + 12, dosDate, true);
    view.setUint32(cursor + 14, item.crc, true);
    view.setUint32(cursor + 18, item.dataBytes.length, true); // Compressed size
    view.setUint32(cursor + 22, item.dataBytes.length, true); // Uncompressed size
    view.setUint16(cursor + 26, item.nameBytes.length, true);
    view.setUint16(cursor + 28, 0, true); // Extra field length
    out.set(item.nameBytes, cursor + 30);
    out.set(item.dataBytes, cursor + 30 + item.nameBytes.length);
    cursor += 30 + item.nameBytes.length + item.dataBytes.length;
  }

  const centralDirOffset = cursor;

  // Central directory headers
  for (const item of prepared) {
    view.setUint32(cursor, 0x02014b50, true); // Central directory signature
    view.setUint16(cursor + 4, 0x031e, true); // Version made by (Unix 3.0)
    view.setUint16(cursor + 6, 20, true); // Version needed to extract (2.0)
    view.setUint16(cursor + 8, 0x0800, true); // UTF-8 filename flag
    view.setUint16(cursor + 10, 0, true); // Compression method: 0 (STORE)
    view.setUint16(cursor + 12, dosTime, true);
    view.setUint16(cursor + 14, dosDate, true);
    view.setUint32(cursor + 16, item.crc, true);
    view.setUint32(cursor + 20, item.dataBytes.length, true);
    view.setUint32(cursor + 24, item.dataBytes.length, true);
    view.setUint16(cursor + 28, item.nameBytes.length, true);
    view.setUint16(cursor + 30, 0, true); // Extra length
    view.setUint16(cursor + 32, 0, true); // Comment length
    view.setUint16(cursor + 34, 0, true); // Disk number start
    view.setUint16(cursor + 36, 0, true); // Internal attributes
    view.setUint32(cursor + 38, 0, true); // External attributes
    view.setUint32(cursor + 42, item.offset, true); // Local header offset
    out.set(item.nameBytes, cursor + 46);
    cursor += 46 + item.nameBytes.length;
  }

  // End of central directory record (EOCD)
  view.setUint32(cursor, 0x06054b50, true);
  view.setUint16(cursor + 4, 0, true); // Disk number
  view.setUint16(cursor + 6, 0, true); // Central dir disk
  view.setUint16(cursor + 8, prepared.length, true); // Disk entries
  view.setUint16(cursor + 10, prepared.length, true); // Total entries
  view.setUint32(cursor + 12, centralTotalSize, true); // Central directory size
  view.setUint32(cursor + 16, centralDirOffset, true); // Central directory offset
  view.setUint16(cursor + 20, 0, true); // Comment length

  return out;
}

export function createZipBlob(entries: ZipInputEntry[]): Blob {
  const bytes = buildZipBytes(entries);
  const copy = new Uint8Array(bytes.byteLength);
  copy.set(bytes);
  return new Blob([copy], { type: 'application/zip' });
}
