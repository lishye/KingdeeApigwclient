@echo off
REM 使用 Java 17 运行 TestApp（因为 apigwclient-0.1.5.jar 与 Java 25 不兼容）
"C:\Program Files\Microsoft\jdk-17.0.17.10-hotspot\bin\java.exe" --add-exports java.base/sun.security.action=ALL-UNNAMED -cp ".;apigwclient-0.1.5.jar" TestApp
