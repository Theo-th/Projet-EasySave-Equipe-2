# Projet EasySave
> Projet de Génie Logiciel - Solution de sauvegarde de fichiers sécurisée.

Ce projet a pour but de concevoir et réaliser un logiciel de sauvegarde (Easy Save) permettant de gérer des travaux de backup de manière performante et fiable.

## 👥 L'Équipe (Groupe 4)

* **Membre 1** (@pseudo) - *Rôle / Lead Dev*
* **Membre 2** (@pseudo) - *Rôle / UI-UX*
* **Membre 3** (@pseudo) - *Rôle / Qualité & Tests*
* **Membre 4** (@pseudo) - *Rôle / Documentation*

---

## 🌿 Stratégie de Branches (Workflow)

Nous utilisons une structure simplifiée pour garantir la stabilité du projet :

1. **`main`** : Branche de **production**. Elle contient uniquement le code stable et testé. C'est la branche utilisée pour les livrables finaux.
2. **`develop`** : Branche d'**intégration**. Tout le développement converge ici. Elle représente l'état le plus avancé du projet en cours.
3. **`feature/nom-de-la-tâche`** : Branches **temporaires**. Chaque nouvelle fonctionnalité ou correction se fait sur une branche dédiée.

### Schéma de travail :

`feature/` ➔ `develop` ➔ `main`

---

## 🛠️ Guide Git 

### Créer une nouvelle fonctionnalité

```bash
# 1. Se mettre sur develop et récupérer le dernier code
git checkout develop
git pull origin develop

# 2. Créer sa branche de travail (ex: ajout de la fonction de log)
git checkout -b feature/systeme-logs

# 3. Travailler, puis indexer et commiter
git add .
git commit -m "feat(logs): ajout de l'écriture des logs en JSON"

# 4. Envoyer la branche sur GitHub
git push origin feature/systeme-logs

```

### Fusionner son travail (Pull Request)

* Une fois le `push` effectué, allez sur GitHub.
* Ouvrez une **Pull Request (PR)** de votre branche `feature/` vers la branche `develop`.
* **Validation :** Un autre membre de l'équipe doit relire le code (Code Review) avant le "Merge".
* Une fois fusionnée, vous pouvez supprimer votre branche `feature/`.

---

## 📋 Bonnes Pratiques de Code (Génie Logiciel)

### 1. Convention de Commits

Nous suivons la norme *Conventional Commits* pour un historique lisible :

* `feat(...)`: Nouvelle fonctionnalité.
* `fix(...)`: Correction d'un bug.
* `docs(...)`: Documentation (README, etc.).
* `refactor(...)`: Amélioration du code sans changer le comportement.

> *Exemple : `feat(ui): création de la barre de progression*`

### 2. Qualité et Propreté

* **Nommage :** Variables et fonctions en anglais (ou français, selon le choix du groupe), de manière explicite. (ex: `calculateRemainingTime()` au lieu de `calc()`).
* **DRY (Don't Repeat Yourself) :** Toute logique répétée doit être isolée dans une fonction réutilisable.
* **KISS (Keep It Simple, Stupid) :** On privilégie la clarté à la complexité technique inutile.
* **Gestion des erreurs :** Chaque accès aux fichiers doit être protégé (blocs `try...catch`) pour éviter les crashs lors des sauvegardes.

---

## 🚀 Installation et Lancement

1. **Clonage du projet :**
```bash
git clone https://github.com/votre-compte/easy-save.git

```


2. **Configuration :**



3. **Exécution :**



