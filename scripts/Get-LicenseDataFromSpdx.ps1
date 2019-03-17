$commit = '2eb50e0db6cf79ba054a518f800a61816ae978c8'
$baseUrl = "https://raw.githubusercontent.com/spdx/license-list-data/$commit/json/licenses.json"

(New-Object System.Net.WebClient).DownloadFile($baseUrl, "$PSScriptRoot/../input/spdx-licenses.json")
