param($installPath, $toolsPath, $package, $project)

function RemoveConfigSection($ProjectDirectoryPath)
{
	$appConfigPath = Join-Path $ProjectDirectoryPath "app.config"
	if (!(Test-Path -Path $appConfigPath))
	{
		$appConfigPath = Join-Path $ProjectDirectoryPath "web.config"
		if (!(Test-Path -Path $appConfigPath))
		{
			Write-Host "app.config (web.config) file not found."
			return
		}
	}
	
	Write-Host "config is '$appConfigPath'." 
			
	$xml= New-Object System.Xml.XmlDocument
	$xml.Load($appConfigPath)
		
	#
	$configSections = $xml.SelectSingleNode('configuration/configSections')
    # if configSection is absent then return
	if ($configSections -eq $null) 
	{
		return
	}

	# sectionGroup name="amba"
	$ambaSectionGroup = $configSections.SelectSingleNode('sectionGroup[@name="amba"]')
    if ($ambaSectionGroup -eq $null)
	{
		return
	}		

	# section name="amba.report"
	$ambaReportSection = $ambaSectionGroup.SelectSingleNode('section[@name="amba.report"]')
    if ($ambaReportSection -eq $null)
	{
        #delete sectionGroup if no children then return
        if ($ambaSectionGroup.ChildNodes.Count -le 0 )
        {
            $configSections.RemoveChild($ambaSectionGroup)   
            $xml.Save($appConfigPath)
        }
		return
	}	

    #otherwise just delete section w/name="amba.report"
    $ambaSectionGroup.RemoveChild($ambaReportSection) 	
	#delete sectionGroup if no children then return
    if ($ambaSectionGroup.ChildNodes.Count -le 0 )
    {
        $configSections.RemoveChild($ambaSectionGroup)   
    }
	$xml.Save($appConfigPath)

}

RemoveConfigSection -ProjectDirectoryPath ([System.IO.Directory]::GetParent($project.FullName))