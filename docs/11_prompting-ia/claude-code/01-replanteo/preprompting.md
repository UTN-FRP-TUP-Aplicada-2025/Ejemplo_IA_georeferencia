no esta funcionando , voy a redactar un replanteo de historias de usuarios y escenarios

te voy a enunciar las historias y vos corregiras, armaras en función un plan para que claude code revise primero la documentación ajuste segun estos puntos y luego arregle el codigo para que cumpla con la documentación /docs, y finalmente corra los test, revise , corrija hasta el exito y finalmente repase los script para lanzar todos los servicios y lance la aplicación en el celular, mueve esos script necesarios a una carpeta /script/launch-all y los script de test puros muevelos a la ruta /scritp/test-all, arregla las ruta en todos estos scripts

Sobre la aplicación android:

1- el usuario quiere poder centrar el mapa para ver su posición en el mapa.
2- el usuario quiere poder visualizar su posición actual en el mapa para poder agregar una foto
3- el usuario quiere poder ampliar y visualizar el rango o radio del marker para poder incluir otras fotos en ese mismo marker.
4- el usuario quiere poder desde el marcadorr (haciendo click) abrir un dialogo con un carrusel con todas las fotos añadidas, en el mismo debe poder agregar una descripcón y titulo del marcador. Al ampliarla debería poder tambien agregar una descripción por foto 
5- el usuario quiere interactuar con la aplicación sin conexión a internet subir o añadir fotos a los markers para cuando encuentre conexión se sincronice con el sistema.
6- el usuario quiere poder quitar fotos desde el carrucel.
7- el usuario quiere poder ver ampliada fotos desde el carrucel.
8- el usuario quiere poder ver e iniciar la sincronización de la apliación.
9- el usuario quiere poder ver y revisar los marcadores creados, editar el carrusel de fotos
10- el usuario quierepoder eliminar el marcador al seleccionarlo
11- el usuario quiere poder compartir una o mas fotos del carrusel a traves de alguna red social, 

escenerarios.
1- al sincronizar encuentra markers que se superonen en sus regiones en el back, no resuelve nada, lo toma como  un marcador nuevo a añadir a la base de datos.
2- cuando la aplicación inicia, el mapa no inicia, debe mostrar un mensaje mapa no iniciado.
3- cuando la aplicación inicia, no puede centrar el mapa por permisos, debe esperar y solicitar el permiso de gps y ubicacion antes de seguir.

Sobre el front.
Se espera la misma historias de usuario que en la aplicación android, salvando que las solicitudes de permisos son sobre el navegador. y ademas:
12- el usuario quiere poder descargar localmente las fotos de un carrusel , 
13- el usuario debe querer poder subir una foto al carrusel desde el front.

escenearios,
4- la foto a subir desde el navegador no tiene exif , no importa, porque queda vinculada al marcador.

fases posibles especializarlo segun el caso
1- entrar en contexto base
1-revisar y actualizar corrigiendo la documentación las historias propuestas, revisar otra documentación para que sea consistente con este planteo como casos de usos y visión y demas documentación complementaria 
2- revisar nuevamente la documentación para que sea coherente
3- generar un contexto  de referencia de la planificación generar las fases de de codeo y testing. Finalizado el sprint, realizar test consturyendo los bat script y ejecutandolos  , corregir si es necesario hasta el exito., hacer el push al cerrar sprint.
4- generar cada uno de los prompts para ejecutar cada uno de los sprint planificado
5- crear o modificar los script bat de lanzado del servicio y aplicación en el celuar conectado generando un test end2end , capturando pantallas y analizando si cumple lo propuesto
