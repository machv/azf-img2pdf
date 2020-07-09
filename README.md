# Azure Function for converting image(s) to PDF


## Deploy via PowerShell

```
$resourceGroupName = "dev"
$functionAppName = "img2pdf"
$deploymentFile = New-TemporaryFile

# Create package
Remove-Item $deploymentFile.FullName
Compress-Archive -Path "./pic2pdf/bin/Release/netcoreapp3.0/publish/*" -DestinationPath $deploymentFile.FullName

# Publish
$functionApp = Get-AzWebApp -ResourceGroupName $resourceGroupName -Name $functionAppName
Publish-AzWebapp -WebApp $functionApp -ArchivePath $deploymentFile

# Cleanup
Remove-Item $deploymentFile.FullName
```

## Sample flow

![Flow](doc/flow.png "Flow")
