This certificate is only an example. Please use your own.

Double click the pfx on a windows mc and use the 1234 password to install. 

Add the certificates to the nx project for example in the **/certs** folder

Update the nx project.json file:

```json
"serve": {
    "executor": "@angular-devkit/build-angular:dev-server",
    "options": {
    "browserTarget": "ui:build",
    "sslKey": "certs/dev_localhost.key",
    "sslCert": "certs/dev_localhost.pem",
    "port": 4201
},
```
1. create pfx 
openssl pkcs12 -export -in dev_localhost.pem -inkey dev_localhost.key -out pkcs12bff.pfx
2. from windows search box, search for certificate and select 'Manage User Certificates'
3. Under 'Trusted Root Certification Authorities/Certificates', right click 'All Tasks -> Import', select pfx, then enter '1234' password

