# Ejemplo_IA_georeferencia


00_run_all.bat
 ├── FASE 1 → 05_run_tests.bat          (unit + integración + e2e)
 ├── FASE 2 → 06b_start_services.bat    (API:5000 + Web:5001)
 ├── FASE 3 → 07b_deploy_and_capture.bat
 │             ├── 03_build_android.bat
 │             ├── 04_install_android.bat
 │             ├── adb reverse tcp:5000 (tunel USB)
 │             └── screencap × 5 pantallas
 └── FASE 4 → 08_validate_screenshots.bat → validation_report.txt


 06b_start_services.bat
 Levanta API :5000 y Web :5001 en ventanas separadas, mata puertos previos y hace health check

 07b_deploy_and_capture.bat
 Build + install + lanza com.companyname.geofoto.mobile + 5 capturas del flujo principal

 08_validate_screenshots.bat
 Verifica existencia e integridad (>10KB) de cada captura y genera test-results/validation_report.txt

 00_run_all.bat
 Orquestador: ejecuta las 4 fases en orden, detiene el pipeline ante cualquier error

 revisa los documentos /docs , enumera los objetivos del sistema que describe , los sitemas que describe y que tecnologia usa

 ahora arma un plan para correr todos los test, invocar los scripts para correr los servicios y enviar la aplicacion al celular,(hay uno solo conectado por usb), y capturar pantallas controlando que cumpla con lo establecido