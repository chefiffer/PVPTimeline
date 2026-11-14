# PvP Timeline

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [한국어](README.ko-KR.md) | [Русский](README.ru-RU.md) | [Português (Brasil)](README.pt-BR.md) | [Español](README.es-ES.md) | [ไทย](README.th-TH.md) | [Tiếng Việt](README.vi-VN.md) | [日本語](README.ja-JP.md) | [Deutsch](README.de-DE.md) | **Français**

Une chronologie personnelle et en temps réel des actions JcJ dans FINAL FANTASY XIV.

Visualisez sur une même chronologie l'utilisation des actions, les lancements, le temps de recharge global (GCD), le verrouillage d'animation, vos propres animations et les retours d'effet. Vous pouvez ainsi revoir quand une action a été utilisée, quand ses effets ont été reçus et quelles aptitudes ont été intercalées.

![Aperçu de PvP Timeline en jeu](assets/pvp-timeline-preview.gif)

> Ceci est une version de test. Les mises à jour du jeu peuvent perturber l'observation et l'affichage, et certains stades d'action peuvent encore manquer de retour. La chronologie présente des observations locales, pas une reconstruction exacte de la détection des impacts sur le serveur. Elle n'automatise aucune action.

## Fonctionnement

![Observation locale et flux des messages](assets/how-pvp-timeline-works.png)

Le schéma sépare les messages échangés entre le client et le serveur des informations temporelles observées localement. PvP Timeline enregistre les demandes d'action acceptées, les états de lancement et de recharge visibles localement, le verrouillage d'animation, la lecture de vos animations, les messages d'effet reçus et les instantanés de vos statuts. Les pistes colorées reprennent les significations de l'affichage par défaut.

Le plugin n'envoie aucun paquet de combat supplémentaire et ne peut pas consulter le processus de décision interne du serveur. Une pression physique qui ne devient jamais une demande d'action du jeu n'est pas observable. L'horodatage du retour d'effet correspond à la réception par le client local et inclut les délais du réseau, du serveur et du client.

## Fonctions

- **Pistes d'actions séparées :** les grandes icônes du bas représentent les sorts, techniques d'arme et certaines actions spéciales ; les petites icônes du haut représentent les aptitudes sur deux lignes au maximum.
- **Pistes temporelles distinctes :** GCD, temps de lancement, verrouillage d'animation et lecture d'animation sont enregistrés séparément.
- **Retour d'effet :** un losange rose signale dégâts, soins, application de statut ou autre retour pris en charge. Les coups critiques et coups nets ajoutent une légère lueur à l'icône d'origine.
- **Effets différés :** les détonations automatiques et phases suivantes prises en charge sont reliées à l'utilisation initiale, ou affichées avec une icône translucide sans inventer une nouvelle pression.
- **Vos statuts :** historique compact de certaines améliorations et altérations observées.
- **Apparence personnalisable :** verrouillage traversable par les clics, disposition horizontale ou verticale, défilement inversé, couleurs, opacité, tailles et échelle générale.
- **Pause et inspection :** figez l'affichage pour revoir les actions récentes ou effacez les données pour recommencer.

L'observation est limitée aux zones prises en charge de la **jetée de l'Antre des loups, de Crystalline Conflict, du Front et des Ailes rivales**. Elle n'est pas active dans les zones JcE ordinaires. Les nouvelles cartes exigent une mise à jour de la liste de compatibilité.

## Prise en main

### Installation

Un environnement capable d'exécuter les plugins **Dalamud API 15** est nécessaire. Utilisez une version compatible avec le jeu et Dalamud actuels.

1. Installez et lancez XIVLauncher avec Dalamud activé.
2. Ouvrez **Dalamud Settings → Experimental**.
3. Ajoutez cette URL de dépôt personnalisé :

   ```text
   https://raw.githubusercontent.com/chefiffer/dalamud-plugins/main/repo.json
   ```

4. Dans l'installateur, recherchez **PVPTimeline** et installez-le.
5. Ouvrez les paramètres avec la commande :

   ```text
   /pvptimeline
   ```

6. Rendez-vous à la jetée de l'Antre des loups ou sur une carte JcJ compatible et utilisez des actions.

### Premier réglage

Désactivez d'abord **Combat only** et testez sur un mannequin à la jetée de l'Antre des loups :

1. Déverrouillez l'interface dans **Display**, puis déplacez-la et redimensionnez-la.
2. Dans **Layout**, réglez les tailles des icônes GCD/aptitudes, les positions et l'épaisseur des barres.
3. Activez **Locked** une fois le placement terminé afin que les clics traversent la chronologie.

Une chronologie verrouillée ne peut pas être cliquée. Utilisez `/pvptimeline` pour rouvrir les paramètres ; lorsqu'elle est déverrouillée, un clic droit fonctionne aussi.

## Lire la chronologie

### Icônes et temps

`0s` est l'instant d'observation actuel ; les valeurs négatives représentent le passé. La grille et les libellés restent fixes tandis que les actions et barres se déplacent vers l'historique.

La piste inférieure à grandes icônes ne signifie pas que toutes ses actions utilisent le GCD : certaines Transcendances et actions spéciales y figurent aussi. Une barre GCD bleue n'est tracée que lorsqu'une recharge partagée est réellement observée.

| Apparence par défaut | Signification |
| --- | --- |
| Barre bleue | Temps de recharge global (GCD) observé |
| Barre verte | Intervalle de lancement observé localement |
| Courte barre jaune vif | Verrouillage d'animation, distinct du GCD |
| Barre violette | Intervalle observé de lecture de votre animation |
| Losange rose plein | Retour direct : dégâts, soins, statut ; pour certains déplacements vérifiés, peut aussi indiquer un accusé du serveur |
| Losange rose vide | Retour tel qu'un effet visuel ou une variation de jauge ; ne prouve pas seul des dégâts ou des soins |
| Icône translucide | Lancement non confirmé ou effet réellement déclenché sans utilisation manuelle correspondante ; ne signifie pas une nouvelle pression |
| Lueur de l'icône | Coup critique, coup net ou coup critique net issus de vrais messages d'effet |
| Fine barre grise (désactivée par défaut) | Recharge indépendante de l'action, pas le GCD |

Toutes les couleurs sont modifiables. Une action multicible peut produire des résultats différents ; l'icône fusionne les indicateurs observés pour cette utilisation. Une lueur de coup critique net ne signifie pas que toutes les cibles en ont reçu un.

Récupération et Porteur d'ombre possèdent aussi de **courtes barres bleues de recharge indépendante** sur la piste des aptitudes. Elles restent indépendantes et ne transforment pas ces actions en actions GCD.

### Pourquoi une barre violette ou un losange manque-t-il parfois ?

Les barres violettes reposent sur l'observation réelle de vos animations. Observation manquante, association incertaine, pause ou remplacement peuvent supprimer, interrompre ou raccourcir une barre. Un marqueur de remplacement n'apparaît que si une autre action a effectivement écrasé l'animation ; une fin normale n'ajoute pas de losange orange.

Les losanges roses exigent également un vrai retour d'effet. Le plugin n'invente rien à partir de la description si le retour manque, ne peut pas être associé de façon fiable ou appartient à une phase non prise en charge.

**L'absence de barre ou de marqueur ne prouve pas l'échec de l'action.** Le GCD ou le verrouillage d'animation ne remplacent pas la durée de lecture de l'animation.

## Lire les lignes de statut

Elles montrent surtout les **statuts réellement présents sur votre personnage**, quelle que soit leur source. L'utilisation d'une action par un allié ne suffit pas à prouver que vous avez reçu son effet.

Les améliorations utilisent des icônes de début et de fin reliées par une barre :

- l'icône de début se déplace avec le temps sans être dupliquée au bord gauche ;
- arrivée au bord, elle rejoint la file ; si la durée restante est connue, un compte à rebours de deux chiffres au plus s'affiche sans unité ni zéro ;
- les icônes sont classées par fin, les plus proches de l'expiration étant à droite ;
- **une icône soulignée d'un petit trait rouge est un point de fin** ; elle peut apparaître dans la zone future avant l'expiration et ne signifie pas que le statut est déjà terminé ;
- après la fin, ce point se déplace vers le passé et le segment disparaît lorsqu'il rejoint le début.

Les sources peuvent être enregistrées séparément. Actualisations, cumuls et suppressions suivent la liste réelle des statuts ; le plugin n'invente pas de cumuls à partir du nombre d'actions.

Tous les statuts ne sont pas affichés. Le plugin filtre les améliorations de jobs vérifiées et exclut certains indicateurs de sélection, de préparation ou d'enchaînement. Ce filtre est encore incomplet. Ouvrez une issue si vous souhaitez voir un autre effet.

**Seuls les statuts et comptes à rebours confirmables sont affichés.** Sans durée restante lisible, aucun compte à rebours n'apparaît. Les conditions telles que « en combat » ne sont pas encore évaluées séparément.

## Paramètres courants

| Emplacement | Options |
| --- | --- |
| Haut des paramètres | Langue, échelle, pause/reprise, effacement |
| Display | Activation, verrouillage traversable, combat uniquement, masquage en cinématique, lignes de statut, observation native, orientation, arrière-plan |
| Layout | Tailles d'icône, hauteur/espacement, positions et épaisseurs GCD/aptitudes, positions lancement/animation |
| Tracks | Visibilité et couleurs des pistes, recharges courtes, aptitudes, attaques automatiques |
| Time | Pixels par seconde, historique, espace futur, grille et subdivisions |
| Feedback | Couleurs des lueurs, marqueur de remplacement, opacité des effets déclenchés |

Saisissez les valeurs ou utilisez la molette au-dessus d'un champ. L'échelle générale va de **0,2 à 2,0×**, texte compris.

L'interface prend en charge le chinois simplifié/traditionnel, l'anglais, l'allemand, le français et le japonais. Elle suit d'abord la langue de Dalamud, avec choix manuel possible. Les noms utilisent les ressources multilingues disponibles et peuvent revenir à l'anglais, à la langue du client ou à un ID. Les noms des joueurs ne sont pas traduits. Modifiez `ui-localization.txt` à côté de la DLL puis rechargez le plugin pour corriger les textes d'interface ; ce fichier ne contrôle pas les noms d'actions ou de statuts.

## FAQ

### La chronologie n'est pas visible

Vérifiez que vous êtes dans une zone JcJ compatible, que le plugin est actif et contrôlez **Combat only** et **Hide in cutscene**. Consultez **Diagnostics** si nécessaire. Une mise à jour du jeu peut exiger une correction de compatibilité.

### Pourquoi les pressions répétées ne s'affichent-elles pas toutes ?

La chronologie montre l'utilisation observable des actions, pas les clics physiques. Une pression qui ne produit aucune demande du jeu est invisible ; les tentatives échouées n'ajoutent pas d'icônes.

### Pourquoi les dégâts automatiques utilisent-ils une icône translucide ?

Un effet causé par vous peut arriver sans enregistrement d'utilisation manuelle. Une détonation automatique inflige de vrais dégâts sans nouvelle pression. Les phases associées de façon fiable actualisent l'action initiale ; les autres effets réels peuvent rester translucides.

### L'horodatage est-il celui de la détection du serveur ?

Non. Il correspond à la réception par votre client et inclut tous les délais de traitement. La lecture d'animation n'est pas non plus l'instantané d'impact du serveur ; la fin d'un lancement ne détermine pas le premier instant de déplacement possible.

### Les données sont-elles envoyées ou les parties complètes sauvegardées ?

Non, pas automatiquement. Des données récentes et limitées restent en mémoire, puis sont réinitialisées au changement de zone ou à l'effacement. Les diagnostics sont copiés dans le presse-papiers uniquement sur demande et peuvent contenir des ID et des horaires : vérifiez-les avant partage.

## Limites et précautions

- La fenêtre d'association est de **14 secondes** au maximum ; les retours trop tardifs ou ambigus ne sont pas attribués de force.
- L'historique est limité à 256 entrées et 120 secondes ; les statuts ont aussi des limites.
- L'observation des animations se concentre sur votre personnage et ne reconstruit pas entièrement les animations des cibles, projectiles ou effets au sol.
- L'observation native utilise des interfaces internes du jeu ; une mise à jour peut la casser ou affecter la stabilité. La désactiver ne supprime pas forcément tous les points d'observation.
- Plugin tiers non officiel, sans garantie de sécurité du compte ni d'absence de plantage. Respectez les règles du jeu et de la plateforme.

## Signaler un problème

Indiquez version du plugin, région/version du jeu, langue, job et action, déclenchement manuel ou automatique, résultat attendu/réel et étapes. Ajoutez si possible une capture ou un GIF, les diagnostics d'effet pour les associations et des logs Dalamud expurgés pour le chargement. Ne partagez jamais jetons, mots de passe ou journaux privés sans rapport.

## Remerciements

La présentation, le verrouillage et la personnalisation s'inspirent de :

- [ActionTimelineEx](https://github.com/Ted-Jin-Lab/ActionTimelineEx)
- [ActionTimelineReplacement](https://github.com/Loskh/ActionTimelineReplacement)

PvP Timeline n'est pas une version officielle de ces projets. Les icônes du jeu et éléments associés appartiennent à leurs ayants droit.

## Licence

PvP Timeline est placé sous GNU Affero General Public License v3.0 (AGPL-3.0). Consultez [LICENSE](https://github.com/chefiffer/PVPTimeline/blob/main/LICENSE).

Les mentions de droits et de licence du projet amont sous MIT sont conservées dans [THIRD-PARTY-NOTICES.txt](https://github.com/chefiffer/PVPTimeline/blob/main/THIRD-PARTY-NOTICES.txt).
