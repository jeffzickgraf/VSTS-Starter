#used to create an AppSettings file on the VSTS build server during a build. Takes in credentials and the cloud location

param([string] $perfectoCloud = "", [string] $perfectoUsername = "", [string] $perfectoPassword = "", [string] $isWindTunnelEnabled="true")

# Create a new XML File with config root node
[System.XML.XMLDocument]$oXMLDocument=New-Object System.XML.XMLDocument
# New Node
[System.XML.XMLElement]$oXMLRoot=$oXMLDocument.CreateElement("appSettings")
# Append as child to an existing node
$oXMLDocument.appendChild($oXMLRoot)

# Add cloud key 
[System.XML.XMLElement]$oXMLCloud=$oXMLRoot.appendChild($oXMLDocument.CreateElement("add"))
$oXMLCloud.SetAttribute("key","PerfectoCloud")
$oXMLCloud.SetAttribute("value", $perfectoCloud)

# Add username key 
[System.XML.XMLElement]$oXMLUser=$oXMLRoot.appendChild($oXMLDocument.CreateElement("add"))
$oXMLUser.SetAttribute("key","PerfectoUsername")
$oXMLUser.SetAttribute("value", $perfectoUsername)

# Add password key 
[System.XML.XMLElement]$oXMLPassword=$oXMLRoot.appendChild($oXMLDocument.CreateElement("add"))
$oXMLPassword.SetAttribute("key","PerfectoPassword")
$oXMLPassword.SetAttribute("value", $perfectoPassword)

# Add IsWindTunnelEnabled key 
[System.XML.XMLElement]$oXMLWindTunnel=$oXMLRoot.appendChild($oXMLDocument.CreateElement("add"))
$oXMLWindTunnel.SetAttribute("key","IsWindTunnelEnabled")
$oXMLWindTunnel.SetAttribute("value", $isWindTunnelEnabled)


# Save File
$oXMLDocument.Save("VSTSDigitalDemoTests\appsettings.config")
