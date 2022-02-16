using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _note : MonoBehaviour
{
    /*
     * 
    +
    + clean coding
        UpdateDesiredSpeed
    
    + cleaning nitro
    + stop using nitro
        + time to change nitroMaxSpeed to BaseMaxSpeed
    
    + test baking time increse so much

    + max distance between near tracker and car = 1 frame
    + modified updateFarTracker
    + test count in VertexPath

    + research wheelCollider without using library => to limit high speed
    + Thing about elements which can limit car speed
    + thinking a bit about large number
    + fix speed of far Tracker
    + implement nitro effect without duration

    + clear affects of nitro
    
    + understanding how near tracker effect
    + go back to normal moz to use Gizmos
    + cos and tan for approachingCornerAngle
    + research approachingCornerAngle with stiffness
    + min speed at corner 
    + calc cautiousnessRequired with approachingCornerAngle and with minLookAhead
    + dynamic lookAhead: = time 
    
    + make smooth running of tracker

    + calculate distance between two points in the path:
    by calculate distanceTravelled
    
    Course: Make a driving game in unity
    + Car Audio: should create audio Source => to make 3D audio

    + bug: high speed and go to the HardBarrier

    [CORE] to make car drive better
    + Car Drive Type: Rear Wheel Drive, also make full torgue better => avoid lagging between 4 wheels
    + Stiffness of front wheels higher than stiffness of front wheels

    base lecture: UserInterface UI > minimap
    How to create Minimap with marker: 
    double Road, Road Copied's layer = minimap. 
    make Road Copied white or any color
    create CarMarker, its layer = minimap
    in mainCamera set culling Mask not include minimap


    
    research 
    +wheel collider
    +CarController.cs
    +CarUserControl.cs
    +CarAudio.cs
    +CarAIController.cs
    
    để điều khiển xe chạy trơn chu: lecture 60, 61
    SaveScript.cs from lecture 28. Speed UI
    AICar 63,64


    xe: rigidbody > dynamic 
    Thử chạy xe trên terrain => chạy được => terrain có collider và là static rigidbody


    1 số đầu ra là nâng cao lúc này:
    + chay xe trên 1 mặt dốc, hay bất kì không phải là mặt phẳng
     */
}
