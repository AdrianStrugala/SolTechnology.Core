resource "azurerm_sql_server" "sqlserver" {
  name                         = "talecodesqlserver"
  resource_group_name          = azurerm_resource_group.rg.name
  location                     = azurerm_resource_group.rg.location
  version                      = "12.0"
  administrator_login          = "sa"
  administrator_login_password = "password_xxddd_2137"

}

resource "azurerm_mssql_database" "sqldb" {
  name           = "TaleCode"
  server_id      = azurerm_sql_server.sqlserver.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  max_size_gb    = 4
  read_scale     = true
  sku_name       = "S0"
  zone_redundant = true

}