properties {
	$base_directory = Resolve-Path .
	$src_directory = "$base_directory\Source"
	$sln_file = "$src_directory\Amba.Report.sln"
	$target_config = "Release"
	$framework_version = "v4.5"
	$output_directory = "$base_directory\Build"
	$dist_directory = "$base_directory\Distribution"
	$xunit_path = "$src_directory\packages\xunit.runners.2.0.0-rc3-build2880\tools\xunit.console.exe"
	$nuget_path = "$src_directory\.nuget\nuget.exe"
	$nl = [Environment]::NewLine

	$buildNumber = 0
	$version = "0.1.0"
	#$preRelease = "alpha"
	$preRelease = $null
}

FormatTaskName (("-"*25) + "[{0}]" + ("-"*25))

Task default -Depends Clean, Build, Test, NugetPackage


Task Build -Depends UpdateVersion, Clean {
	Write-Host "Building Amba.Report.sln" -ForegroundColor Green
	Exec { msbuild /nologo /verbosity:q $sln_file /p:Configuration=$target_config /p:TargetFrameworkVersion=$framework_version /p:OutDir=$output_directory }
	
	Write-Host $nl
}

Task Clean -Depends UpdateVersion{
	Write-Host "Cleaning output directories" -ForegroundColor Green
	
	rmdir $output_directory -ea SilentlyContinue -recurse
	rmdir $dist_directory -ea SilentlyContinue -recurse

	Exec { msbuild $sln_file /t:Clean /p:Configuration=Release /v:quiet } 
	Write-Host $nl
}

Task UpdateVersion {
	$vSplit = $version.Split('.')
	
	if($vSplit.Length -ne 3)
	{
		throw "Version number is invalid. Must be in the form of 0.0.0"
	}
	$major = $vSplit[0]
	$minor = $vSplit[1]
	$patch = $vSplit[2]
	

	$assemblyFileVersion =  "$major.$minor.$patch.$buildNumber"
	$assemblyVersion = "$major.$minor.0.0"

	Write-Host "Updating version" -ForegroundColor Green
	Write-Host "AssemblyFileVersion: $($assemblyFileVersion)"
	Write-Host "AssemblyVersion: $($assemblyVersion)"

	$versionAssemblyInfoFile = "$src_directory/VersionAssemblyInfo.cs"
	"using System.Reflection;" > $versionAssemblyInfoFile
	"" >> $versionAssemblyInfoFile
	"[assembly: AssemblyVersion(""$assemblyVersion"")]" >> $versionAssemblyInfoFile
	"[assembly: AssemblyFileVersion(""$assemblyFileVersion"")]" >> $versionAssemblyInfoFile
	Start-Sleep -s 1
	Write-Host $nl
}

Task Test -Depends Build{
	Write-Host "Testing Amba.Report.Test" -ForegroundColor Green
	
	$project = "Amba.Report.Test"
	mkdir $output_directory\xunit\$project -ea SilentlyContinue | Out-Null
	.$xunit_path "$output_directory\$project.dll" -html "$output_directory\xunit\$project\index.html"
	
	Write-Host $nl
}


Task NuGetPackage -depends ILMerge{
	$vSplit = $version.Split('.')
	if($vSplit.Length -ne 3)
	{
		throw "Version number is invalid. Must be in the form of 0.0.0"
	}
	$major = $vSplit[0]
	$minor = $vSplit[1]
	$patch = $vSplit[2]
	$packageVersion =  "$major.$minor.$patch"
	if($preRelease){
		$packageVersion = "$packageVersion-$preRelease"
	}
	
	if ($buildNumber -ne 0){
		$packageVersion = $packageVersion + "-build" + $buildNumber.ToString().PadLeft(5,'0')
	}

	Write-Host ("Creating NuGet Package version {0}" -f $packageVersion) -ForegroundColor Green
	
	Copy-item $src_directory\Amba.Report.nuspec $dist_directory
	Copy-item $output_directory\Amba.Report.xml $dist_directory\lib\net45\
	
	New-Item $dist_directory\tools -Type Directory | Out-Null
	Copy-item $src_directory\install.ps1 $dist_directory\tools\
	Copy-item $src_directory\uninstall.ps1 $dist_directory\tools\

	Exec { .$nuget_path pack $dist_directory\Amba.Report.nuspec -BasePath $dist_directory -o $dist_directory -version $packageVersion }

}

Task ILMerge -Depends Build{
	Write-Host "Merging is not required." -ForegroundColor Green
	New-Item $dist_directory\lib\net45 -Type Directory | Out-Null
	Copy-item $output_directory\Amba.Report.dll $dist_directory\lib\net45\
	Write-Host $nl
}

