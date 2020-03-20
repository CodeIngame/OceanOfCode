#The Goal
Vous pilotez un sous-marin et vous savez qu'un ennemi est présent près de vous parce que vous écoutez sa communication sur les fréquences radio.
Vous ne savez pas exactement où il se trouve mais vous pouvez entendre tous ses ordres.
Vous et votre adversaire avez 6 points d'impact. Lorsque les points de vie d'un joueur atteignent 0, le joueur perd.

#Rules
##Definitions

- Les sous-marins se déplacent sur une carte constituée d'eau et d'îles.
	Ils ne peuvent se déplacer que sur des cellules contenant de l'eau. Ils peuvent partager la même cellule sans entrer en collision.
- La carte est composée de 15 cellules en largeur et de 15 cellules en hauteur.
	Les coordonnées commencent à (0,0) qui est la cellule supérieure gauche de la carte.
- La carte est divisée en 9 secteurs, qui contiennent chacun 25 cellules (5x5 blocs de cellules).
Le secteur supérieur gauche est le 1, le secteur inférieur droit est le 9.

##Beginning of the game
Au début du jeu, vous recevrez une carte (15x15 cases) qui indique la position des îles.
Les îles sont des obstacles.
Vous ne pouvez pas vous déplacer ou tirer à travers les îles. Ensuite, vous déciderez où vous voulez placer votre sous-marin en indiquant une coordonnée (x,y).

##Each turn
Il s'agit d'un jeu au tour par tour, ce qui signifie que chaque joueur joue un tour après l'autre.
Le joueur avec l'id 0 commence. Pendant votre tour, grâce à l'analyse des fréquences radio, vous recevrez une indication de ce que votre adversaire a fait.
Par exemple, vous pouvez recevoir qu'il s'est déplacé vers le nord.
C'est à vous d'utiliser cette précieuse information pour détecter où il se trouve.
Ensuite, vous devez effectuer au moins une action.

##Actions
À chaque tour, vous devez effectuer au moins une action.
Vous pouvez effectuer plusieurs actions en les enchaînant à l'aide du tuyau |.
Mais vous ne pouvez utiliser chaque type d'action qu'une seule fois par tour (vous ne pouvez vous déplacer qu'une seule fois par tour, pas plus).
Si vous ne parvenez pas à produire une action valide, vous ferez surface à ce tour.

##Move
Une action de déplacement déplace votre sous-marin 1 cellule dans une direction donnée (nord, est, sud, ouest) et charge une puissance de votre choix.
Lorsque vous vous déplacez, vous devez respecter les règles suivantes :
- Vous ne pouvez pas vous déplacer à travers les îles
- Vous ne pouvez pas vous déplacer sur une cellule que vous avez déjà visitée auparavant

Vous pouvez décider de ce que vous voulez charger.
Différents appareils nécessitent un montant différent de frais pour être prêts. Dans cette ligue, vous ne pouvez charger que la torpille.

##Surface
En utilisant la surface, vous réinitialiserez le cheminement des cellules visitées afin de pouvoir vous déplacer librement vers une cellule que vous avez déjà visitée.
Mais le surfacing a un impact majeur : votre adversaire saura dans quel secteur vous faites surface et vous perdrez 1 point de vie.

##Torpedo
Une torpille nécessite 3 actions de charge pour être prête. Lorsqu'elle est complètement chargée, la torpille peut être tirée à une position arbitraire dans l'eau, dans un rayon de 4 cellules.
Cela permet à la torpille de contenir les coins et de contourner les îles, mais pas de les traverser.
Les dégâts de l'explosion sont de 2 sur la cellule elle-même et de 1 sur tous les voisins (y compris les diagonales).