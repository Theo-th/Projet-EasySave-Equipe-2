# Projet EasySave - Équipe 2
> Projet de Génie Logiciel - Solution de sauvegarde de fichiers sécurisée.

Ce projet consiste à concevoir un logiciel de sauvegarde performant permettant de gérer des travaux de backup de manière fiable. La version actuelle est une application **Console** utilisant **.NET**.

---

## 👥 L'Équipe (Groupe 2)

D'après les contributeurs du dépôt :

* **Théo Thivillier** (@Theo-th)
* **maelpitois** (@maelpitois)
* **antoineP-it** (@antoineP-it)
* **enzogilles** (@enzogilles)

---

## Fonctionnalités (Livrable 1 - version 1.0)

Le logiciel permet de créer jusqu'à **5 travaux de sauvegarde**.

### Caractéristiques d'un travail :
* **Nom** de la sauvegarde.
* **Répertoire source** et **répertoire cible**.
* Support des **disques locaux**, **externes** et **lecteurs réseaux**.
* **Type de sauvegarde** : Complète ou Différentielle.
* **Intégrité** : Sauvegarde de tous les éléments (fichiers et sous-répertoires).

### Points clés :
* **Internationalisation** : Logiciel utilisable en **Anglais** et **Français**.
* **Exécution flexible** : Lancement d'un travail unique ou exécution séquentielle de l'ensemble.

## Fonctionnalités (Livrable 2 - version 1.1)
* **Ajout** de la possibilité d'écrire les journaux dans un fichier .xml au lieu de .json en fonction du choix de l'utilisateur.


## Fonctionnalités (Livrable 2)
Cette version introduit une interface graphique complète ainsi que des fonctionnalités avancées de sécurité et de paramétrage, conformément aux exigences des versions 1.1 et 2.0.

### 1. Interface et Gestion des Travaux
* **Interface Graphique (GUI)** : Abandon du mode Console pour une interface utilisateur intuitive (WPF etAvalonia).
* **Travaux Illimités** : Création et gestion d'un nombre **illimité** de travaux de sauvegarde.
* **Gestion Complète** : Création, modification et suppression des travaux de sauvegarde.
* **Modes d'exécution** :
  * Lancement d'une sauvegarde unique.
  * Exécution **séquentielle** de l'ensemble des travaux.

### 2. Sécurité et Chiffrement
Intégration du logiciel externe **CryptoSoft** pour sécuriser les données sensibles.
* **Chiffrement sélectif** : Seuls les fichiers dont les extensions ont été définies par l'utilisateur sont chiffrés.
* **Clé de chiffrement** : Définition personnalisée de la clé dans les paramètres.
* **Performance** : Le temps de chiffrement est calculé et ajouté aux logs (en ms).

### 3. Sûreté de Fonctionnement (Logiciel Métier)
Le logiciel intègre une sécurité passive pour ne pas perturber l'activité professionnelle.
* **Détection Logiciel Métier** : L'utilisateur peut définir un logiciel métier à surveiller.
* **Interdiction d'exécution** : Si le logiciel métier est détecté en cours d'exécution, EasySave empêche le lancement des travaux pour garantir l'intégrité des fichiers.

### 4. Logs et Suivi en Temps Réel
Le système de journalisation a été amélioré pour répondre aux standards XML et JSON (Livrable 1.1).
* **Formats supportés** : Choix du format des logs entre **JSON** et **XML** via les paramètres.
* **État en temps réel** : Fichier d'état unique mettant à jour la progression, le fichier en cours et l'état actif/inactif.
* **Journalier** : Historique complet incluant l'horodatage, les chemins, la taille et le temps de transfert (incluant le temps de chiffrement).
* **Chemin personnalisé** : Possibilité de définir un dossier spécifique pour le stockage des logs.

### 5. Paramètres Généraux
Un panneau de configuration permet de gérer l'environnement de l'application :
* **Langue** : Bascule instantanée entre **Français** et **Anglais**.
* **Configuration** : Gestion des extensions à chiffrer, du chemin des logs et du logiciel métier.
---

## 🛠️ Utilisation et Commandes

L'exécutable peut être lancé directement via le terminal.

### Exemples d'exécution :
* **Plage de sauvegardes** : `EasySave.exe 1-3` (exécute les travaux 1 à 3).
* **Sélection spécifique** : `EasySave.exe 1;3` (exécute les travaux 1 et 3).

---

## Stratégie de Branches (Workflow)

Nous utilisons une structure simplifiée pour garantir la stabilité du projet :

1. **`main`** : Branche de production contenant uniquement le code stable et testé.
2. **`develop`** : Branche d'intégration où tout le développement converge.
3. **`feature/nom-de-la-tâche`** : Branches temporaires pour chaque nouvelle fonctionnalité.

### Schéma de travail :
`feature/` ➔ `develop` ➔ `main`

---

## Bonnes Pratiques de Code

### 1. Convention de Commits
Nous suivons la norme *Conventional Commits* pour un historique lisible :
* `feat(...)`: Nouvelle fonctionnalité.
* `fix(...)`: Correction d'un bug.
* `docs(...)`: Documentation (README, etc.).
* `refactor(...)`: Amélioration du code sans changement de comportement.

### 2. Qualité et Propreté
* **DRY (Don't Repeat Yourself)** : Toute logique répétée est isolée dans une fonction réutilisable.
* **KISS (Keep It Simple, Stupid)** : On privilégie la clarté à la complexité technique inutile.
* **Gestion des erreurs** : Accès aux fichiers protégé par des blocs `try...catch`.
* **Nommage** : Variables et fonctions explicites (ex: `calculateRemainingTime()`).

---

## 💾 Installation

1. **Clonage du projet :**
```bash
git clone https://github.com/Theo-th/Projet-EasySave-Equipe-2.git
```

2. **Compilation et Lancement** : Ouvrez la solution avec Visual Studio ou utilisez le CLI .NET :
```bash
dotnet build
dotnet run
```
