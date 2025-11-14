AC_Shield est une solution de protection des utilisateurs contre les appels non solicités. Les SBC audiocodes proposent des protections internes contre un certain nombre de menaces, mais n'apporte pas de mécanisme automatique lorsque les appels non solicités proviennent d'un partenaire de confiance (ex opérateur).

AC_Shield propose les fonctionalités ci dessous:

- Réception des CDR du SBC et maintien d’une table interne des numéros appelants à bloquer (avec un délai configurable de déblocage automatique)
- Génération périodique d’un fichier csv contenant les numéros bloqués, que l’on peut importer dans un dial plan
- Envoi d’un rapport email (une fois par jour) avec la liste des numéros bloqués
- Exposition d’une API REST qui permet au SBC de demander si le numéro est bloqué. Via une call setup rule, le SBC peut alors rejeter l’appel.
>NB : Si AC_Shield est indisponible, cela ne bloque pas les traitements d’appel par le SBC.
