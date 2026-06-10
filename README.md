# SAB-patcher
Teeny Tiny C# app that breaks StartAllBackX64.dll, giving you unlimited Free Trial days.<br/>
Verified supported versions (others might work too):
All versions from: `3.9.16` to: `3.9.23`
   
## How to use
1. Run the `SAB-patcher.exe` (preferably as Administrator).
2. (Optional) Pass in an argument by launching via cmd or powershell.
3. Done.

To verify that it completed successfully, open StartAllBack after _the patcher tells you that it has done its job.._ and go to the "About tab". Once your `Trial days left:..` reach `0`, it will simply go into the negatives and you will keep all of the perks. You may also restart your computer (can sometimes help with the program loading the DLL).
This is how a proper install looks (I'm already way past the 100 day mark):

<img width="852" height="853" alt="image" src="https://github.com/user-attachments/assets/752c306d-58a6-40fd-a600-32692e246237" />

## Arguments
The program comes with 2 arguemnts you can pass when opening via commandline:
  - (no argument) - Finds and patches the DLL (no arguments).         
  - `--test`      - Finds and patches the DLL in a set of "test directories". (you can find them in the source)
  - `--restore`   - Finds and restored the original DLL.

## What does it actually do
It is set to find `StartAllBackX64.dll` in `C:\Program Files\StartAllBack`, `C:\...\AppData\Local\StartAllBack`, or whatever directory the program is launched from (unless you're using `--test`). 
When the DLL is found:
 1. Makes a backup with a `.bak` extension (for `--restore`).
 2. Looks for a sequence of bytes and replaces the first 3 with `0x31, 0xC0, 0xC3`. (you can find what the bytes are in the `Project.cs` file near the top)
 3. Exits.
    _It does some other stuff like kill/restarting processes that are preventing it from editing the `StartAllBackX64.dll` so that it actually works.._

## Troubleshooting
Last time **I** tested the app was on 10/02/2026 on **StartAllBack version 3.9.21**.
If the app tells you that it couldn't find the `StartAllBackX64.dll`, put the `SAB Patcher.exe` in the same folder as the DLL.
If the app tells you that it couldn't edit the file (for some reason - usually because it's being used by another process), try using [File Locksmith](https://www.edtittel.com/blog/powertoys-file-locksmith-works-well.html) (or [Powertoys](https://apps.microsoft.com/detail/XP89DCGQ3K6VLD?hl=en-US&gl=US&ocid=pdpshare)) to kill the process that is causing the issues and **restart the patcher**.

## ⚠️ Disclaimer
This tool is for educational and personal use only. Modifying binaries may 
violate the Terms of Service of the software being patched. Use this at your 
own risk. The author is not responsible for any system instability or 
data loss.
