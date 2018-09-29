This is a basic demo application for [Dissonance Voice Chat](https://www.assetstore.unity3d.com/#!/content/70078?aid=1100lJDF), a VoIP asset for Unity3D. Dissonance supports a large number of network backends (including custom ones) so that it can easily plug into any existing multiplayer game.

This demo uses the integration for the [WebRTC Network](https://assetstore.unity.com/packages/tools/network/webrtc-network-47846?aid=1100lJDF) asset, there is a "signalling server" which performs the initial setup of each session but after that it's entirely peer to peer. Unfortunately this p2p nature means that the demo can sometimes fail to setup a session if you're behind a particularly complicated NAT or aggressive firewall. If this is preventing you from successfully trying the demo feel free to email admin@placeholder-software.co.uk and we'll see if we can help.

# Download

If you want to try the demo before purchasing Dissonance to evaluate voice quality you can download precompiled windows binaries [here](https://static.placeholder-software.co.uk/.Dissonance/Demo.zip).

# Compile

If you want to compile this for yourself follow these instructions:

### Required Setup

First you must clone this repository. The project will _not compile_ after the initial clone. You must purchase and install [Dissonance Voice Chat](https://www.assetstore.unity3d.com/#!/content/70078?aid=1100lJDF) from the Unity Asset Store into the project.

# Using The Demo

### Creating A Session

1. Run the application.
2. Click "host".
3. An invite code will be displayed - share this code with other users for them to join the session.
4. Press 'v' to talk.

### Joining A Session

1. Run the application.
2. Click "join".
3. Paste the invite code you received from the host.
4. After a short delay you should join the session.
5. Press 'v' to talk.

### Advanced Configuration

Click the cog icon in the bottom left corner to access the settings window. From here you can configure custom signalling servers and ICE servers. If you want to run a custom ICE server [coturn](https://github.com/coturn/coturn) is an excellent project which performs both STUN and TURN. STUN helps peers connect to each other through layers of NAT, if this fails then TURN is a fallback which relays packets between the peers.