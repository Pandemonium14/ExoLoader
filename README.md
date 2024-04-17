# ExoLoader

(this readme is outdated! Will update sometime)

Mod for I Was a Teenage Exocolonist that makes adding custom characters easier.

Uses BepInEx (https://github.com/BepInEx/BepInEx)to inject the code (included in the releases)

Current features: 
- Adding a character to the list without modifying the Chara.tsv file (easier + more compatible)
- Loading new sprites into the game for the portrait and the character sprite during stories
- Friendship points actually functional
- Add a map spot to the map for your character that triggers their stories

Coming soon (tm) :
- Use one of the game's skeleton animation with another texture to use for your map spot OR use a static image instead

# Installation

Just put the contents of the Exocolonist folder in the release zip into your Exocolonist game folder, and launch the game.

# Adding a character

In the CustomCharacters located in the Exocolonist folder is a CharacterTemplate folder. Use it to figure out how you should structure your own character folder and how the images should be named. (Do ping me in the ecc discord if you have questions!! I'll just be happy you're using this)

Make sure you're not saving data.json as a .txt file!

For now the handling of the story sprite size is a bit rough. I recommend following the size and proportion of template2_angry.png but I didn't experiment too much, so tell me if you figure out what makes it work or not work.

# Finding bugs?

Ping me (@pandemoniium) in the ECC discord, I'll take a look and try to fix!
