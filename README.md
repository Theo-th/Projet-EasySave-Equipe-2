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
