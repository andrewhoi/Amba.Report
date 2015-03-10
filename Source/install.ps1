param($installPath, $toolsPath, $package, $project)

function AddConfigSection($ProjectDirectoryPath)
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
	
	# add configSection
	$configSections = $xml.SelectSingleNode('configuration/configSections')
	
	if ($configSections -eq $null) 
	{
		$configSections = $xml.CreateElement('configSections')
		$firstChild = $xml.configuration.get_FirstChild()
		$xml.configuration.InsertBefore($configSections, $firstChild)
	}
	# add sectionGroup name="amba"
	$ambaSectionGroup = $configSections.SelectSingleNode('sectionGroup[@name="amba"]')
	if ($ambaSectionGroup -eq $null)
	{
		$ambaSectionGroup = $xml.CreateElement('sectionGroup')
		$ambaSectionGroup.SetAttribute('name', 'amba')
		$configSections.AppendChild($ambaSectionGroup )
	}
	# add section name="amba.report"
	$ambaReportSection = $ambaSectionGroup.SelectSingleNode('section[@name="amba.report"]')
	if ($ambaReportSection -eq $null)
	{
		$ambaReportSection = $xml.CreateElement('section')
		$ambaSectionGroup.AppendChild($ambaReportSection )
	}
	# update in both cases
	$ambaReportSection.SetAttribute('name', 'amba.report')
	$ambaReportSection.SetAttribute('type', 'Amba.Report.ConfigSection, Amba.Report')
	# 
	$xml.Save($appConfigPath)
}

AddConfigSection -ProjectDirectoryPath ([System.IO.Directory]::GetParent($project.FullName))