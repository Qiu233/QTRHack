# QTRHack(still under development)
QTRHack is a rewritten version of QTRHacker which can be found in another repo.  
Actually I didn't think of any name that fits this project, so temporarily removed the last two letters just to distinguish from the older one.  

# What's new
Currently nothing new in functionality but a little in techniques and architecture.  

New GUI: using WPF with MahApps.Metro, which means the UI would be more effective and easy to use.  
New Architecture: More expandable to support both vanilla and TML of different version.  

Promising new techniques:  
1. Running code on managed thread.(still in test, trying to find another way to do this)
2. Loading managed assembly at runtime.(if the previous one done)
3. Rejit post-jit methods (across process, difficult)
4. To be added.

# Overview
## Structure of Solution
* `Keystone.Net`: Managed wrapper for keystone assembler, used when hack trys to generate code(i.e. to hook).
* `QHackLib`: A fully rewritten version of the previous one, providing all kinds of basic techniques.
* `QTRHack.Kernel`: The replacement of `QTRHacker.Functions`, providing the framework of hack.
* `QTRHack.Core`: Providing the **base** implementations of `GameObjects`, which must be rewritten/overriden/inherited in sub-projects.
* `QTRHack.UI`: New UI implementation using WPF with MahApps.Metro.
* `QTRHack.Core.XXX_YYYY`: The **actual** implementations of `GameObjects`.

## QHackLib
As seen, `QHackLib` has no prefix like `QTRHack` or `QTRHacker`, which means it is an independent project from this hack and hence is reusable for other hack operations.  
Actually `QHackLib` can be considered a fair wrapper for [`clrMD`](https://github.com/microsoft/clrmd) as well as an abstract layer between managed and unmanaged world.  
### Core Concepts
* `QHackLib.Context` represents the per-process context of hack, only through which can you access the target process. 
To get an instance of this class, call `QHackLib.Context.Create()`. 
When hack is all done, call `QHackLib.Context.Dispose` to release resource this instance uses.  
* `QHackLib.DataAccess` provides all types of data access to target process as well as memory allocation and reclamation. 
Use generic method as possible because those are the fastest way.  
* `QHackLib.HackObject` is a `dynamic` wrapper for objects in clr. You can access its fields simply by name and index it(if is array) using `int[]` as indexes. 
No matter how you access it, you will get another instance of `HackObject` even if the field is typed `unmanaged`. 
But the good news is that you can convert a `HackObject` to an `unmanaged` type by casting implicitly or explicitly from `dynamic`.
* `QHackLib.Assemble.AssemblySnippet.FromClrCall()` has been rewritten. Now it can completely automatically decide how to pass arguments when calling functions. 
The only price is that you should specify the `return buffer` on your own. For more information about this, see [botr: Return-buffers](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/clr-abi.md#return-buffers). 
If you're not sure about this, **then do not call methods that return a structure!**

# Get Started
Here are several tutorials to help you understand this solution.(assuming you're playing vanilla 1.4.3.2).  
You should:
1. Download or clone the repository.
2. Create a new `.NET Framework Console` project in the solution.(assuming name is `XXX`)
3. Add references to `QHackLib`/`QTRHack.Kernel`/`QTRHack.Core`/`QTRHack.Core.VNL_1432`
4. `Ctrl + F5`, nothing to run, just to create `bin` directory.
5. Copy `./refdlls/keystone.dll` to `./XXX/bin/Debug` or `./XXX/bin/Release`.

In the main method, try the following code snippets.
## 1.Attach to game
This is the framework.
```C#
using (HackKernel kernel = HackKernel.Create(Process.GetProcessesByName("Terraria")[0]))//Assuming that your game process is Terraria.exe
{
	kernel.SetCore(BaseCore.GetCore(kernel.GameContext, "VNL-1.4.3.2-0"));//Load 1.4.3.2 core
}
```

## 2.Get my player's StatLife
```C#
using (HackKernel kernel = HackKernel.Create(Process.GetProcessesByName("Terraria")[0]))
{
	kernel.SetCore(BaseCore.GetCore(kernel.GameContext, "VNL-1.4.3.2-0"));
	GameObjectArray<BasePlayer> players = kernel.GetStaticGameObject<GameObjectArray<BasePlayer>>("Terraria.Main", "player"); //Get Main.player (Player[])
	int myPlayer = kernel.GetStaticGameObjectValue<int>("Terraria.Main", "myPlayer"); //Get Main.myPlayer (int)
	BasePlayer curPlayer = players[myPlayer]; //Get Main.player[Main.myPlayer]
	Console.WriteLine(curPlayer.StatLife); // Output the statLife
}
```
