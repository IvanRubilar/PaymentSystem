# PaymenSystem

Este proyecto implementa un sistema de pagos que permite registrar trx mediante la carga de archivos, para su posterior
procesamiento y generación de nómina.

## Funcionalidades principales

- Carga de archivo CSV de entrada 
- Validación de datos requeridos y formato correcto
- Inserción en base de datos mediante procesamiento paralelo
- Generación de CSV de salida de nómina
- Registro detallado de logs, tiempos de ejecución y errores
- Archivos separados para registros inválidos o duplicados 
- Ejecución automática diaria (cron job simulado) a las 23:00 (hora Chile)
- Exposición de API REST para ejecutar el proceso manualmente

## Tecnologías utilizadas

- .NET 8
- ASP.NET Core API
- Worker Service
- Entity Framework Core (PostgreSQL)
- CsvHelper
- Dependency Injection / Logging

##  Ejecución Manual

POST /api/csv/procesar?nombreEntrada=REGISTRO.TRX.csv&nombreSalida=NOMINA.PAGOS.custom.csv

- `nombreEntrada`: obligatorio, nombre del archivo de entrada dentro de `archivos/`
- `nombreSalida`: opcional, nombre de salida. Si se omite, se genera con formato por fecha.

## Worker automático

- Configurado para ejecutarse todos los días a las 23:00 hora Chile
- Puede forzarse ejecución inmediata activando `ModoDebug` en `appsettings.json`:

## Bitácora de actividades

|ID  |                        Actividad	                                  |            Comentario
=== =================================================================================================================================================================
|1   |  Configuración del proyecto base (.NET API + Worker)               | Se optó por separar la lógica en un Worker y un API para ejecución automática y manual.  
|2   |  Diseño e implementación de modelo Transferencia y                 |	Separación clara entre entidad persistente y datos de entrada desde CSV.
|3   |  Configuración de Entity Framework Core con PostgreSQL	          | PostgreSQL fue elegido por su  compatibilidad con EF Core y disponibilidad libre.
|4   |  Implementación de carga de CSV y procesamiento básico	          | Se usó CsvHelper por su compatibilidad con .NET, flexibilidad y facilidad para mapear columnas.
|5   |  Validación de campos obligatorios y registros vacíos	          | Para asegurar integridad de los datos antes de insertar en la base.
|6   |  Implementación de inserción en bd con procesamiento paralelo      |	Mejora significativa en tiempos de ejecución con cargas grandes con (AsParallel). 
|7   |  Generación de archivo CSV de resumen agrupado                     | Se cumple con lo solicitado. 
|8   |  Manejo de registros inválidos                                     | Para Mejorar la trazabilidad y transparencia del procesamiento.
|9   |  Control de duplicados en archivo y base de datos	              | Para garantizar unicidad de trx y evitar errores de lógica.
|10  |  Exposición de endpoint REST (/api/csv/procesar)	                  | Cumple con requerimiento de ejecución bajo demanda desde cualquier sistema.
|11  |  Diseño de logs detallados en consola y archivo EJECUCION...LOG	  | Se mejoró la trazabilidad y depuración. Se incluyó cantidad de hilos y duración.
|12  |  Implementación de ejecución automática diaria(simulación cron job)|	Se optó por Task.Delay y cálculo de diferencia horaria debido a la necesidad de compatibilidad 
|13	 |  Inclusión de ModoDebug para ejecutar Worker sin esperar las 23:00 |	Aceleró las pruebas y permitió ajustes rápidos durante desarrollo.
|14	 |  Estandarización de nombres de archivos de entrada/salida          |	Mejora de legibilidad, orden y compatibilidad. para API se permite cambios de nombre
|15	 |  Carga de datos masiva (CSV de 1.000 registros aleatorios)         |	Validación de rendimiento y comportamiento ante volumen.


Desarrollado por: Iván Rubilar Díaz