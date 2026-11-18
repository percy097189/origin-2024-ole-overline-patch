# Origin 2024 OLE Overline Patch

A reversible Windows patch for unwanted horizontal bars above text in Origin graphs embedded in PowerPoint.

After copying an Origin graph with **Ctrl+J** and pasting it into PowerPoint, the graph may initially look correct. Opening the embedded graph in Origin and closing the editor can add bars above axis titles, legends, and text labels. This tool patches Origin's text renderer so future embedded-object updates use the corrected rendering path.

## !! Important Notice !!

- This unofficial patch is a fallback of the official Origin 2024b patch. Only use it when you are not available to the official update patch.

- This patch is only tested on Origin 2024 SR1 x64, build **10.1.0.178**, on both Windows 10 and Windows 11 OS. 

- Your browser may warn you about the released executive when you download it, but it should be save. You can compile it by yourself if you want.

- This tool is 100% vibe coded, use it at your own risk.

## Compatibility

- Windows x64 with .NET Framework 4.x.
- Tested with Origin 2024 SR1 x64, build **10.1.0.178**.
- Installation folder containing `Origin64.exe` and the supported `otext.dll`.
- Write access to that installation folder. An installation under Program Files may require an elevated terminal.

The tool validates the entire DLL using SHA-256. Other DLL versions are rejected.

| State | SHA-256 of otext.dll |
| --- | --- |
| Original | `E2B19C4E73DFAC30A62FE73B822365746198EA8289AA28B60BDD3BD4D1C62C3B` |
| Patched | `322E09EF6CF5FF2F1C929839A73BCB0B67510B574A3AA10E1C4EE531AD72E7E3` |

Validation covered the local installation and PowerPoint workflow described below. Compatibility with a second computer remains to be verified.

## Download

Download `Patch-OriginOleOverline.exe` and `SHA256SUMS.txt` from the repository's **Releases** page. The same executable is available in `dist/`.

The release executable is unsigned. Its SHA-256 is

```text
8CAC64952EB078B34ED8F861ABC53542A60F027312D06DB7E580F8E6B8DAF08C
```

To check the download in PowerShell, run

```powershell
Get-FileHash .\Patch-OriginOleOverline.exe -Algorithm SHA256
```

## Apply

1. Save your work and close Origin, including editors opened from PowerPoint.
2. Open MS Powershell **as administrator** in the folder containing the downloaded executable.
3. Run the following command, replacing the example path with your Origin installation folder.

```powershell
.\Patch-OriginOleOverline.exe apply "C:\Program Files\OriginLab\Origin2024"
```
If you have changed the installation destination, replace "C:\Program Files\OriginLab\Origin2024" to your folder that contains Origin64.exe

The tool saves a verified original backup beside the DLL as
`otext.dll.before-origin-ole-overline-patch`. Keep this file for restoration.

Check the installed state with

```powershell
.\Patch-OriginOleOverline.exe status "C:\Program Files\OriginLab\Origin2024"
```

Start Origin again after applying the patch. Processes that already loaded the DLL need to be reopened.

For an existing PowerPoint graph whose cached preview already contains bars, open the embedded graph, close Origin, and save the presentation. Origin regenerates the preview through the patched renderer. Graphs remain editable.

## Restore

Save your work, close Origin, and run

```powershell
.\Patch-OriginOleOverline.exe restore "C:\Program Files\OriginLab\Origin2024"
```

Restoration requires the verified original backup. Repeated apply and restore operations are supported.

Exit codes are `0` for success, `1` for an operation error or unsupported DLL, and `2` for invalid arguments.

## How the patch works

Runtime tracing located the unwanted lines in `otext.dll`. In the affected rendering path, the text-decoration routine loads the text run's formatting flags and executes `bts esi, 0x0c`, forcing the overline flag when an additional context flag is set.

The patch replaces that four-byte instruction with four NOPs. Each text run retains its explicitly specified formatting.

| Property | Value |
| --- | --- |
| File | `otext.dll` |
| File offset | `0x27643` |
| Relative virtual address | `0x28243` |
| Original bytes | `0F BA EE 0C` |
| Replacement bytes | `90 90 90 90` |

The tool checks the input DLL hash, instruction bytes, staged output hash, current installed hash, and backup hash before replacing the DLL. Replacement uses Windows' `File.Replace` operation. A named mutex serializes tool instances in the same Windows session.

The initialization event that originally sets the extra rendering-context flag remains unresolved.

## Validation

The following checks were performed locally on Origin 2024 SR1 x64.

- A fresh automation instance produced 14 line records in a test graph before patching. After patching, the sole remaining line belonged to text deliberately formatted with `\ab(...)`.
- Opening and closing an existing PowerPoint Origin object regenerated its EMF preview with zero unwanted line records, down from five.
- A second edit changed the X-axis title to `Patch round trip`. PowerPoint saved the new title, retained the `Origin95.Graph` object type, and generated a preview with 19 text records and zero line records.
- Apply, repeated apply, restore, repeated restore, and rejection of a modified unsupported DLL passed on disposable file copies.
- The saved PowerPoint preview was visually inspected.

## Build from source

Run `build.cmd` on Windows. It uses the .NET Framework C# compiler at
`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe` and writes the x64 executable to `build/`.

```powershell
.\build.cmd
.\build\Patch-OriginOleOverline.exe status "C:\Program Files\OriginLab\Origin2024"
```

The source uses framework libraries only. Compiler-generated PE metadata can make a local build's file hash differ from the release hash.

## Background

OriginLab documents the same class of Office-embedded graph text bars in FAQ-1203 and reports a fix in Origin 2024b. This repository provides an independently developed patch for the exact Origin 2024 DLL identified above.

[OriginLab FAQ-1203](https://docs.originlab.com/quick-help/line-hovering-over-texts/)
