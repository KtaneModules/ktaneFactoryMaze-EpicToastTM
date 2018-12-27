using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class moduleScript : MonoBehaviour {

    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable left, right, reset, unlock;
    public Transform leftTransform, rightTransform;
    public TextMesh roomText;
    public SpriteRenderer[] checkmarks, g;
    public GameObject[] doorObjects;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool solved;

    private readonly int[,,] mazes = { { { 1, 2 }, { 2, 3 }, { 3, 4 }, { 4, 0 }, { 0, 1 } },
                                       { { 1, 3 }, { 2, 4 }, { 0, 3 }, { 1, 4 }, { 0, 2 } },
                                       { { 2, 3 }, { 0, 4 }, { 1, 4 }, { 1, 2 }, { 0, 3 } } };
    private readonly string[] rooms = { "Bathroom", "Assembly Line", "Cafeteria", "Room A9", "Broom Closet", "Basement", "Copy Room", "Unnecessarily\nLong-Named Room", "Library",
                                        "Break Room", "Empty Room\nwith Two Doors", "Arcade", "Classroom", "Module Testing\nRoom", "Music Studio", "Computer Room", "Infirmary",
                                        "Bomb Room", "Space", "Storage Room", "Lounge", "Conference Room", "Kitchen", "Incinerator" };
    private string[] usedRooms = { "", "", "", "", "" };
    private int mazeNum, startRoom, currentRoom, keyNum, unlocked = 0;
    private int[] doors = { 0, 0, 0, 0, 0 }; // this number represents which door takes you through the first exit each room; 0 is left, 1 is right
    private readonly int[,,] defaultKeys = { { { 2, 3, 0 }, { 4, 1, 2 }, { 0, 1, 3 }, { 3, 4, 99 }, { 1, 2, 99 } },
                                             { { 3, 4, 0 }, { 0, 1, 1 }, { 2, 0, 4 }, { 3, 1, 99 }, { 4, 0, 99 } },
                                             { { 4, 3, 0 }, { 0, 3, 2 }, { 2, 1, 3 }, { 1, 4, 99 }, { 4, 0, 99 } } };
    // first two numbers of each key is the start/end of the hallway that the key is in; the number after is the room it corresponds to, where 99 is no room.
    private int[,,] currentKeys = { { { 2, 3, 0 }, { 4, 1, 2 }, { 0, 1, 3 }, { 3, 4, 99 }, { 1, 2, 99 } },
                                    { { 3, 4, 0 }, { 0, 1, 1 }, { 2, 0, 4 }, { 3, 1, 99 }, { 4, 0, 99 } },
                                    { { 4, 3, 0 }, { 0, 3, 2 }, { 2, 1, 3 }, { 1, 4, 99 }, { 4, 0, 99 } } };
    private readonly string[] doorStrings = { "left", "right" };
    private int keyHeld; // 5 = no key, 0-4 = keys #0-#4, 99 = useless key. this makes no sense but it works for me

    void Start () {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;

        GenerateModule();
    }

    void Activate()
    {
        left.OnInteract += delegate ()
        {
            if (!solved)
            {
                left.AddInteractionPunch();
                Press(0);
            }
            return false;
        };

        right.OnInteract += delegate ()
        {
            if (!solved)
            {
                right.AddInteractionPunch();
                Press(1);
            }
            return false;
        };

        reset.OnInteract += delegate ()
        {
            if (!solved)
            {
                DebugMsg("You pressed the reset button.");
                reset.AddInteractionPunch();
                GenerateModule();
            }
            return false;
        };

        unlock.OnInteract += delegate ()
        {
            if (!solved)
            {
                unlock.AddInteractionPunch();
                Unlock();
            }

            return false;
        };
    }

    void DebugMsg(string msg)
    {
        Debug.LogFormat("[Factory Maze #{0}] {1}", _moduleId, msg);
    }
    
    void Press(int doorNum)
    {
        int previousRoom = currentRoom;
        currentRoom = mazes[mazeNum, currentRoom, (doors[(currentRoom)] + doorNum) % 2];
        roomText.text = usedRooms[currentRoom];
        
        DebugMsg("You went through the " + doorStrings[doorNum] + " door. You are now in room #" + (currentRoom + 1) + ".");

        for (int i = 0; i < 5; i++)
        {
            if (currentKeys[mazeNum, i, 0] == previousRoom && currentKeys[mazeNum, i, 1] == currentRoom)
            {
                DebugMsg("You picked up a key!");
                Audio.PlaySoundAtTransform("ding", Module.transform);
                if (keyHeld != 5 && keyHeld != currentKeys[mazeNum, i, 2])
                {
                    currentKeys[mazeNum, keyNum, 0] = previousRoom;
                    currentKeys[mazeNum, keyNum, 1] = currentRoom;

                    DebugMsg("You dropped the key you already had.");
                }
                
                keyHeld = currentKeys[mazeNum, i, 2];
                keyNum = i;
                currentKeys[mazeNum, i, 0] = 99;
                break;
            }
        }

        StartCoroutine("DoorAnimation", doorNum);
    }

    void GenerateModule()
    {
        for (int i = 0; i < 3; i++)
        {
            checkmarks[i].enabled = false;
            if (i != 2)
            {
                g[i].enabled = false;
            }
        }

        unlocked = 0;
        keyHeld = 5;
        mazeNum = Random.Range(0, 3);
        startRoom = Random.Range(0, 5);
        currentRoom = startRoom;
        for (int i = 0; i < 5; i++)
        {
            usedRooms[i] = "";
            doors[i] = Random.Range(0, 2);
        }
        for (int i = 0; i < 5; i++)
        {
            int roomNum = Random.Range(0, rooms.Length);
            int rndIncrease = Random.Range(1, rooms.Length);
            while (usedRooms.Contains(rooms[roomNum]))
            {
                roomNum = (roomNum + rndIncrease) % rooms.Length;
            }
            usedRooms[i] = rooms[roomNum];
            
            DebugMsg("Room #" + (i + 1) + " is called the " + usedRooms[i] + ".");
        }
        roomText.text = usedRooms[currentRoom];

        DebugMsg("You are in maze #" + (mazeNum + 1) + ".");
        DebugMsg("You started in room #" + (startRoom + 1) + ".");

        currentKeys = defaultKeys;
    }

    void Unlock()
    {
        if (keyHeld == 5)
        {
            DebugMsg("You attempted to unlock Room #" + (currentRoom + 1) + " without a key.");
        }

        else if (keyHeld == 99)
        {
            DebugMsg("You attempted to unlock Room #" + (currentRoom + 1) + " with a useless key.");
        }

        else
        {
            DebugMsg("You attempted to unlock Room #" + (currentRoom + 1) + " with Key #" + (keyHeld + 1) + ".");
        }
        
        if (keyHeld == currentRoom)
        {
            checkmarks[unlocked].enabled = true;
            DebugMsg("It worked!");
            keyHeld = 5;
            unlocked++;

            if (unlocked == 3)
            {
                Module.HandlePass();
                DebugMsg("You solved the module.");
                Module.enabled = false;
                solved = true;
                roomText.text = "";
                StartCoroutine("DoorAnimation", 2);
            }
        }
        
        else
        {
            Module.HandleStrike();
            DebugMsg("That was wrong, obviously. STRIKE!");
        }
    }

    IEnumerator DoorAnimation(int doorNum)
    {
        if (doorNum == 0)
        {
            for (int i = 0; i < 20; i++)
            {
                leftTransform.transform.Rotate(Vector3.forward, 4.5f);
                yield return new WaitForSeconds(.01f);
            }

            for (int i = 0; i < 20; i++)
            {
                leftTransform.transform.Rotate(Vector3.back, 4.5f);
                yield return new WaitForSeconds(.01f);
            }
        }

        else if (doorNum == 1)
        {
            for (int i = 0; i < 20; i++)
            {
                rightTransform.transform.Rotate(Vector3.forward, 4.5f);
                yield return new WaitForSeconds(.01f);
            }

            for (int i = 0; i < 20; i++)
            {
                rightTransform.transform.Rotate(Vector3.back, 4.5f);
                yield return new WaitForSeconds(.01f);
            }
        }

        else
        {
            for (int i = 0; i < 2; i++)
            {
                g[i].enabled = true;
            }
            
            for (int i = 0; i < 30; i++)
            {
                rightTransform.transform.Rotate(Vector3.forward, 4.5f);
                leftTransform.transform.Rotate(Vector3.forward, 4.5f);
                yield return new WaitForSeconds(.01f);
            }
        }
    }

    public string TwitchHelpMessage = "!{0} press left will go through the left door, and !{0} press right will go through the right. !{0} unlock will attempt to unlock a lock, and !{0} reset will reset" +
                                      "the module.";

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.ToLowerInvariant().Equals("press left"))
        {
            yield return null;
            yield return new KMSelectable[] { left };
        }

        else if (command.ToLowerInvariant().Equals("press right"))
        {
            yield return null;
            yield return new KMSelectable[] { right };
        }

        else if (command.ToLowerInvariant().Equals("unlock"))
        {
            yield return null;
            yield return new KMSelectable[] { unlock };
        }

        else if (command.ToLowerInvariant().Equals("reset"))
        {
            yield return null;
            yield return new KMSelectable[] { reset };
        }

        else
        {
            yield break;
        }
    }
}
