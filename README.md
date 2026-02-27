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

## 🚀 Évolution des Fonctionnalités

### Livrable 1 & 2 (v1.0 à v2.0)
* **Gestion des Travaux** : Passage de 5 travaux maximum à un nombre illimité.
* **Types de Sauvegarde** : Support complet et différentiel.
* **Internationalisation** : Interface disponible en Anglais et Français.
* **Sécurité** : Chiffrement des fichiers via **CryptoSoft** pour des extensions spécifiques.
* **Logiciel Métier** : Détection d'applications interdites pour bloquer le lancement.
* **Logs** : Choix du format entre **JSON** et **XML**.

### Livrable 3 (v3.0 - Version Actuelle)
La version 3.0 introduit des capacités de traitement avancées et une gestion réseau optimisée :
* **Sauvegardes en Parallèle** : Abandon du mode séquentiel pour permettre l'exécution simultanée des travaux.
* **Fichiers Prioritaires** : Gestion d'une liste d'extensions prioritaires traitées avant tout autre fichier.
* **Contrôle de la Bande Passante** : Interdiction de transférer simultanément deux fichiers dont la taille dépasse un seuil **n Ko** paramétrable.
* **Interaction Temps Réel** : Possibilité de mettre en **Pause**, **Play** ou **Stop** (arrêt immédiat) chaque travail individuellement.
* **Progression** : Suivi en temps réel de l'état d'avancement via un pourcentage de progression.
* **Pause Automatique** : Arrêt de tous les transferts en cours si un logiciel métier est détecté, avec reprise automatique dès sa fermeture.
* **CryptoSoft Mono-instance** : Modification de CryptoSoft pour interdire les exécutions simultanées sur un même poste.
* **Centralisation Docker** : Service de centralisation des logs en temps réel via Docker (modes : Local, Centralisé, ou les deux).

---

## 🛠️ Utilisation et Commandes

### GUI

L'exécutable (.exe) peut être lancé via le terminal.

#### Exemples d'exécution :
* **Lancement mode GUI** : `EasySave.exe` 

### Console

L'exécutable peut être lancé dans powershell avec interface console.

#### Exemples d'exécution :
* **Lancement mode console** : `EasySave.exe -console` 

### Terminal

L'exécutable peut être lancé directement via le terminal.

#### Exemples d'exécution :
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

dotnet run --project EasySave.GUI

dotnet run --project EasySave.GUI -- -console

dotnet run --project EasySave.GUI -- 1;2

dotnet run --project EasySave.GUI -- 1-2

```
