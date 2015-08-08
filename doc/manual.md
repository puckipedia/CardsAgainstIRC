So you want to play Cards against IRC? Well, it's not very hard! This manual is split up in a few parts.

# 1. Playing the game
Someone asked you to join a game! Great, he/she will have done most of the difficult work to set up the game. Just type `!join` and you should be entered into the game!
I will not explain the rules of Cards against Humanity, and I presume you know those.

Each round, a person will be appointed to be Czar. This person will be responsible for choosing the best fitting / funniest set of cards. Then, the other players all get a `NOTICE` from the bot, telling them their cards. This message may show up in the currently open buffer, the server buffer, or in the channel you are playing the game in. The message looks like this:

    <CardsAgainstIRC> ​New round! nortti is czar! heddwch, puckipedia, choose your cards!
    <CardsAgainstIRC> ​Current Card: [<kla​nge> stop, _ time!]
    -CardsAgainstIRC- ​0: [$CARD_0] | 1: [$CARD_1] | 2: [$CARD_2] | 3: [$CARD_3] | 4: [$CARD_4] | 5: [$CARD_5] | 6: [$CARD_6] | 7: [$CARD_7] | 8: [$CARD_8] | 9: [$CARD_9]

(`<CardsAgainstIRC>` messages are sent to everyone, `-CardsAgainstIRC-` messages to you specifically)

Here, we have the current card, `[<kla​nge> stop, _ time!]`. Your job, if you aren't the czar, is to choose the card you think fits best or is the funniest. To choose card 2, you send `!card 2`. If a card requires more than one card to fill in, you can give it multiple card numbers.

Once everyone has done this, the list of chosen cards will be sent to everyone:

    <CardsAgainstIRC> ​Everyone has chosen! The card sets are: (nortti - your time to choose)
    <CardsAgainstIRC> ​0. <kla​nge> stop, [$CARD_4] time!
    <CardsAgainstIRC> ​1. <kla​nge> stop, [$CARD_9] time!
    <CardsAgainstIRC> ​2. <kla​nge> stop, [$CARD_2] time!

The czar then sends `!card 1` to choose 1 as the best/funniest card set, and that person wins a point!

    <CardsAgainstIRC3> ​And the winner is... heddwch!
    <CardsAgainstIRC3> ​<kla​nge> stop, [$CARD_9] time!
    <CardsAgainstIRC3> ​Points: heddwch: 2 (1) | puckipedia: 2 | nortti: 4 (0) | <Rando Cardrissian>: 4 (2)

And this repeats until someone hits the limit, and that person is crowned winner!

# 2. Setting up a game
Compared to version 1, CaIv3 has a more complicated setup. However, this does mean there is more flexibility, and the code base is massively improved.
To start a game and activate the bot, send `!start`. You will be automatically joined into the game, so no need to type `!join`. If the bot has `+o`, it will automatically `+v` joined people.

Then, you need to set a limit, with `!limit [num]`. This is the amount of points someone needs to get to win.

Then, you can either add bots or add cards.

## 2.a Adding bots
CaIv3 has support for more complicated bots, and these are easy to write. To add a bot, you first need to need to bot type.
To list the available bot types, type `!bot.types`. This will send a NOTICE with the bot types available. Normally, only `rando`, is available, a bot that chooses random cards and hopes to win that way.
To add a bot, type `!bot.add $id "$name"` (name is optional, and defaults to the bot type). To remove one, type `!bot.remove "$name"`. All active bots have `<>` around their names, to distinguish them from normal players.
To list all players in the game, type `!users`.

## 2.b Adding cards
At the moment, CaIv3 only has built-in support for [Cardcast](https://cardcastgame.com/) card sets. To add these, use `!deck.add cardcast $DECK_ID`, e.g. `!deck.add cardcast EU6CJ`.

(note: I might change the terminology

## 2.c Starting the game
Now that you've done all this, and everyone has joined, you're ready to start the game! Just type `!start` once more to do this.

# 3. The config file

The config file for CaIv3, `config.json`, consists of three parts. `irc` contains the connection information for the bot. `channels` lists the channels the bot should join once started, but if the bot is connected through a bouncer, it will automatically pick those channels up.
The `cardsets` map lists the default 'packs' of decks you can use with `!deckset.add` (and list with `!deckset.list`). These consist of a name (case sensitive!) and an array of decks to add. This is in the format of `!deck.add`, just in an array instead of delimited with spaces.