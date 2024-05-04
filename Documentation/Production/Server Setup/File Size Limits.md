# File Size Limits

This document describes how to configure the large file upload size limit.

## Quick Reference

### Size Units

Configuration and source code should use base 10 (SI) for size.
Refer to the following table:

| Unit          | Amount              | Calculation  |
|---------------|---------------------|--------------|
| Byte (B)      | 1 byte              | 10^0 bytes   |
| Kilobyte (KB) | 1,000 bytes         | 10^3 bytes   |
| Megabyte (MB) | 1,000,000 bytes     | 10^6 bytes   |
| Gigabyte (GB) | 1,000,000,000 bytes | 10^9 bytes   |

### Ciphertext Enlargement

An encrypted message or encrypted file is necessarily larger than it's plaintext form.
Refer to the following table and sections beneath:

| What               | Frequency and Location                       | Size      |
|--------------------|----------------------------------------------|-----------|
| Header length      | Once, at the beginning of every ciphertext   | 4 bytes   |
| Header             | Once, after the header length                | 24 bytes  |
| Chunk length       | At the beginning of every ciphertext chunk   | 4 bytes   |
| Chunk              | As many as it takes to encrypt the plaintext | See below |
| Padding            | The last chunk, if at all                    | See below |
| Authentication Tag | The end of each chunk                        | 17 bytes  |

#### Chunk

The size of a ciphertext chunk is configurable and always subject to change.

The size of a plaintext chunk is currently dictated by the Crypter.Web project.
See [appsettings.json](../../../Crypter.Web/wwwroot/appsettings.json) under `TransferSettings.MaxReadSize`.

The Chunk size should always be a multiple of the Padding size, otherwise every chunk will be padded regardless of size.

#### Padding

All ciphertext chunks are padded up to a multiple of a configurable value.

The padding size is also dictated by the Crypter.Web project.
See [appsettings.json](../../../Crypter.Web/wwwroot/appsettings.json) under `TransferSettings.PadSize`.

#### Doing the math

N chunk count = Ceiling(Plaintext size / Chunk)

Ciphertext length = Header length + Header + N(Chunk length + Chunk + Authentication Tag)

#### Demonstration

Encrypting the word `testing` (any sequence of 64 bytes or less).

L = 7 = length of `testing`

N = 1 = Ceiling(4 / 32704)

113 = 4 + 24 + 1(4 + (ceiling(7 / 64) * 64) + 17)

### Size Chart

| Plaintext Size | Chunk Count | Ciphertext Size |
|----------------|-------------|-----------------|
| <= 64 B        | 1           | 113 B           |
| 10^3 B         | 1           | 1,073 B         |
| 10^6 B         | 31          | 1,000,679 B     |
| 250 * 10^6 B   | 7,644       | 250,160,573 B   |