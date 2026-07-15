cd /d "%~dp0"
set GEN_CLIENT=..\..\..\Tools\Luban\Luban.dll
set CONF_ROOT=.

dotnet %GEN_CLIENT% ^
    -t client ^
    -c cs-newtonsoft-json ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=..\..\GameScripts\Hotfix\AutoGenerate\Tables ^
    -x outputDataDir=output