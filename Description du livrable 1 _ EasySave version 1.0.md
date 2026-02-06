# Description du livrable 1 : EasySave version 1.0

Le cahier des charges de la première version du logiciel est le suivant :
Le logiciel est une application **Console** utilisant **.Net**.

Le logiciel doit permettre de créer jusqu'à **5 travaux de sauvegarde**

### Un travail de sauvegarde est défini par :
* Un nom de sauvegarde
* Un répertoire source
* Un répertoire cible
* Un type (sauvegarde complète, sauvegarde différentielle)

### Fonctionnement général :
* Le logiciel doit être utilisable à minima par des utilisateurs **anglophones et francophones**.
* L'utilisateur peut demander l'exécution d'un des travaux de sauvegarde ou l'exécution séquentielle de l'ensemble des travaux.
* Le fichier exécutable du programme peut être exécuté sur le terminal par une ligne de commande :
    * **Exemple 1 :** « EasySave.exe 1-3 » pour exécuter automatiquement les sauvegardes 1 à 3
    * **Exemple 2 :** « EasySave.exe 1 ;3 » pour exécuter automatiquement les sauvegardes 1 et 3
* Les répertoires (sources et cibles) pourront être sur :
    * Des disques locaux
    * Des disques externes
    * Des lecteurs réseaux
* Tous les éléments d'un répertoire source (fichiers et sous-répertoires) doivent être sauvegardés.