@echo on
@if "%~1"=="" (
  @echo Drop a file onto this .bat file.
  @pause
  @exit /b 1
)

@set "file=%~1"
@set "outdir=C:\Users\OddAdmin\source\repos\osAutoCast\osEffectCompiles"
@set "output=%outdir%\%~n1.ps"

@set /p stype=Pixel or Vertex: 
@set /p version=Shader Version: 
@set /p entrypoint=Entry Point: 

@echo Compiling %~nx1

@"D:\Windows Kits\10\bin\10.0.22000.0\x64\fxc.exe" /T %stype%s_%version%_0 /E %entrypoint% /O3 /Gis /Gfp /Fo "C:\Users\OddAdmin\source\repos\osAutoCast\osEffectCompiles\%~n1.ps" "%~1"
