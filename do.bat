@echo on
@if "%~1"=="" (
  @echo Drop a file onto this .bat file.
  @pause
  @exit /b 1
)

@set "file=%~1"
@set "outdir=C:\Users\OddAdmin\source\repos\osAutoCast\osEffectCompiles"
@set "output=%outdir%\%~n1.ps"

@echo Compiling %~nx1

@"D:\Windows Kits\10\bin\10.0.20348.0\x86\fxc.exe" /T ps_5_0 /E osProgShader_Main /Fo "C:\Users\OddAdmin\source\repos\osAutoCast\osEffectCompiles\%~n1.ps" "%~1"
