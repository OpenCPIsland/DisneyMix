<div align="center">
  
![Disney Mix Icon](https://github.com/Disney-Remixed/DisneyMix/raw/main/Assets/Resources/icons/app_icon.png)

# A recreation/decompilation project of Disney Mix.

</div>


### 🛠 Technical Specifications
* **Unity Version:** `6000.3.28f1`
* **Roadmap:** Currently working on the server software and fixing client if necessary.
* **Unity Roadmap:** Updates beyond **Unity 6.3** are not planned until the release of **Unity 6.7**.
* **Backend:** Based on the **Disney Mix API** (used on Club Penguin Island & Disney Mix)
---

### 📈 Project Status
- [x] **Client status:** Mostly stable, few bugs here and there that need fixing, mostly shaders.
- [x] **Mobile Support:** Stable.
- [x] **Desktop Support:** Stable.
- [x] **Chatting:** Stable.
- [x] **Minigames:** 6/6 games work. Some needs improvements, but they are overall working.
- [x] **Stickers and gags**: Gags and stickers are fully working.
- [x] **Input Systems:** Most custom input bars have been converted to the native Unity Input Field.

---

## Bots and Fake Friends

Disney Mix has local, client side fake friends. They are created when a session is wrapped by [`LocalUserWrapper`](Assets/Scripts/Assembly-CSharp/Mix/Session/Local/LocalUserWrapper.cs), which adds them to the user's friend list and one on one chat threads.

### Characters

The client creates Mixbot and these 23 offline characters:

Baymax, Cheshire, Chopper, Disgust, Dory, Fear, Flash, Hank, Joy, Judy, Marlin, Mike, Nemo, Nick, Olaf, Sabine, Sadness, Sally, Skellington, Stitch, Stormtrooper, Sulley, and Zeb.

They are local objects, not individual server accounts. Offline character IDs use `OfflineCharacter-<name>` (spaces removed); their thread IDs begin with `OfflineCharacterThread:`. The characters are marked as trusted, active bot chat members. Each offline character's costume catalog ID is configured in `LocalUserWrapper`; an unset costume falls back to catalog ID `0`.

Mixbot is a separate fake friend, not an offline character. Its avatar is loaded from `data/Avatar/mixbotdna.txt` through the asset manager and refreshed when the asset manifest changes.

### Chat behavior

- Offline character text replies are generated locally from keyword categories and character specific rules in [`FakeFriendManager`](Assets/Scripts/Assembly-CSharp/FakeFriendManager.cs). The rules cover greetings, farewells, identity, feelings, jokes, facts, and other simple prompts. They are canned/pattern based, unmatched messages receive a generic reply.
- Offline characters accept text, stickers, gags, and game state messages. Text gets a local reply. Sticker and gag replies use cached content from the user's owned items when available, game state messages are accepted without a character reply. Photo and video messages are not supported by the offline character send handler.
- Fake conversations and messages are stored per user in the local `FakeFirstFriendDocumentCollectionApi` database and reloaded into the fake threads on session initialization. They are not server synced.
- Mixbot has a separate scripted onboarding conversation. It is initialized for new users by [`LoginManager`](Assets/Scripts/Assembly-CSharp/Mix/User/LoginManager.cs), progress is saved in the per user key value store (`fakefriend_message_*`, `fakefriend_time_inited`, and related keys). Its prompt text comes from `customtokens.fakefriend.message_<n>` localization entries. The script advances in response to user messages and message types, its random reply check currently returns `false`.

### Maintaining and testing

To add or remove an offline character, update the roster and costume ID map in `LocalUserWrapper`. Add or update its response facts and rules in `FakeFriendManager`, and ensure the costume exists in the content catalog. Mixbot avatar data is maintained separately at `data/Avatar/mixbotdna.txt`. The fake thread send, reply, and persistence behavior is implemented in [`FakeFriendData`](Assets/Scripts/Assembly-CSharp/Mix/FakeFriend/Datatypes/FakeFriendData.cs) and [`FakeThread`](Assets/Scripts/Assembly-CSharp/Mix/FakeFriend/Datatypes/FakeThread.cs).

After signing in, open a fake friend's conversation from the friend list. Fake message persistence is covered by [`FakeFirstFriendDocumentCollectionTests`](Assets/Scripts/Assembly-CSharp/Mix/Test/FakeFirstFriendDocumentCollectionTests.cs).

### Official account bot IDs

Official account bots are separate from the local fake friends above. [`OfficialAccountUtils`](Assets/Scripts/Assembly-CSharp/Disney/Mix/SDK/Internal/OfficialAccountUtils.cs) classifies remote member IDs beginning with `bot-` as bot chat members. The [`Official_Account_Bot`](Assets/Scripts/Assembly-CSharp/Mix/Data/Official_Account_Bot.cs) content sheet model supplies each such bot's `botid` and `icon`.

---

### ⚠️ Known Issues & Missing Content
* **Content Assets:** A significant portion of the original Disney Mix content is lost.
---

### 🚀 Getting Started
1. Clone the repository.
2. Ensure you are using the correct **Unity Editor** version specified above.
3. Add the project to **Unity Hub** and open it.
4. Run `Tools/Mix/AssetBundles/Rebuild from JSON` from the toolbar.
5. To boot the app, open `StaticAssets/Scenes/scn_Main.unity`.
---

### ©️ Credits
Special thanks to the following people who have made this restoration possible:
> [broimluna](https://github.com/broimluna)

> [shinonasada9](https://github.com/shinonasada9)

> [wednesday2024](https://github.com/wednesday2024)
