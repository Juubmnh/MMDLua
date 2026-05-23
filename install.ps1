Stop-Process -Name MikuMikuDance -Force -ErrorAction SilentlyContinue

while ($true)
{
	try
	{
		if (-not (Test-Path -Path ".\MikuMikuDance\MMDLua" -PathType Container))
		{
			New-Item -Path ".\MikuMikuDance\MMDLua" -ItemType Directory -Force | Out-Null
		}
		else
		{
			$fs = [System.IO.File]::Open(".\MikuMikuDance\MMDLua\Dialog.exe", "Open", "Read", "None")
			$fs.Close()
		}
		if (-not (Test-Path -Path ".\MikuMikuDance\plugin\MMDLua" -PathType Container))
		{
			New-Item -Path ".\MikuMikuDance\plugin\MMDLua" -ItemType Directory -Force | Out-Null
		}
		else
		{
			$fs = [System.IO.File]::Open(".\MikuMikuDance\plugin\MMDLua\MMDLua.dll", "Open", "Read", "None")
			$fs.Close()
		}
		break
	}
	catch
	{
		Start-Sleep -Milliseconds 500
	}
}

Copy-Item -Path ".\Dialog\bin\Debug\net8.0-windows\*" -Destination ".\MikuMikuDance\MMDLua" -Recurse -Force
Copy-Item -Path ".\x64\Debug\MMDLua.dll" -Destination ".\MikuMikuDance\plugin\MMDLua" -Force

$ProjectPath = Join-Path -Path $PSScriptRoot -ChildPath MikuMikuDance\UserFile\test_project.pmm
.\MikuMikuDance\MikuMikuDance.exe $ProjectPath