using UnityEngine;
using Unity.Netcode; // Required for Networking
using Unity.Netcode.Components; // Required for NetworkTransform. This is required for the NetworkTransform component, which is used to synchronize the position and rotation of the game object across the network.


// This informs the editior that the game object this script is attached to must have a Rigidbody2D,
// CapsuleCollider2D, and NetworkTransform component. If you try to add this script to a game object
// that doesn't have these components, Unity will automatically add them for you. This is useful
// because it ensures that the game object. You can require any component, even your own custom
// scripts, as long as they are in the same project. You can also require multiple components, as shown in this example.
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(NetworkTransform))]



// Any object that will be dynamic in a scene with net code should probably inhereit from NetworkBehaviour, 
// even if it doesn't have any netcode in it yet. This is because you can easily add netcode to it later
// without having to change the base class, which can be a pain if you have a lot of references to it in
// your code. Additionally, if you want to use any of the built-in netcode features, such as NetworkTransform
// or NetworkAnimator, you will need to inherit from NetworkBehaviour. 
public class NetObjectTemplate : NetworkBehaviour
{


    ///// <Idea for falling objects script>
    /// I recommend setting up the script to track the object’s "state" change—specifically focusing
    /// on the transition from touching to not touching the platform. Start by placing an Empty 
    /// GameObject at the bottom of your object to act as a dedicated ground sensor. Use a physics 
    /// check like Physics2D.OverlapCircle on that sensor to confirm the object is currently grounded. 
    /// Once the fall begins, the script should wait until that sensor returns false, proving it has 
    /// successfully cleared the starting platform and is officially in the air. This 
    /// "touching-to-not-touching" logic is the best way to handle it because it ensures the object 
    /// doesn't accidentally trigger its own explosion or scoring logic while it's still sitting on 
    /// the ledge. Only after it confirms it is no longer touching the platform should it look for 
    /// the next collision; if it hits a "Dog" tag, add the points, but if it hits the ground layer, 
    /// just trigger the destruction.
    /// / </Idea for falling objects script>
    

    //// <Netcode RPCs> 
    /// It is beyond the scope of this project, but I recommend you look into Remote Procedure Calls (RPCs) 
    /// as you level up your development skills. They are essential for networking because they allow a 
    /// script on one player's machine to trigger a function on everyone else’s. Learning how to sync 
    /// actions like explosions or score updates across a network is a huge step toward building solid 
    /// multiplayer games.
    /// / </Netcode RPCs>
    
}
