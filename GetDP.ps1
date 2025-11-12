[cmdletbinding(SupportsShouldProcess)]
param ([Parameter(Mandatory=$true)][string]$Address, [string]$User = "Admin", [string]$Password="Admin")


# call rest API in order to backup dialplan file
$URI="http://$Address/api/v1/files/dialplan"

$securedPassword = ConvertTo-SecureString $Password -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential($User,$securedPassword) 

Invoke-RestMethod -Uri $URI -Credential $cred -Method PUT -ContentType text/csv -InFile "export.csv"