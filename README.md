# SAB-patcher
Teeny Tiny C# app that breaks StartAllBackX64.dll, giving you unlimited Free Trial days.

## How to use
1. Run the `SAB-patcher.exe` (preferably as Administrator).
2. (Optional) Pass in an argument by launching via cmd.exe or powershell.
3. Done.

## Arguments
The program comes with 2 arguemnts you can pass when opening via commandline:
  - (no argument) - Finds and patches the DLL (no arguments).         
  - `--test`      - Finds and patches the DLL in a set of "test directories".
  - `--restore`   - Finds and restored the original DLL.

## What does it actually do
It is set to find `StartAllBackX64.dll` in `C:\Program Files\StartAllBack`, `C:\...\AppData\Local\StartAllBack`, or whatever directory the program is launched from (unless you're using `--test`). 
When the DLL is found:
 1. Makes a backup with a `.bak` extension (for `--restore`)
 2. Looks for a sequence of bytes and replaces the first 3 with `0x31, 0xC0, 0xC3`.
 3. Exits.
