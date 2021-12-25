# QTRHack
**WASTED.**  

This project was once to replace QTRHacker but now has been wasted. Most work on this has been migrated to QTRHacker. 

# Why is it wasted
So fay, QTRHack succeeds to inject Harmony into game process and in turns can apply any patch to game.  
At first I want to make an infrastructure toolkit named "PatchLoader" to support general patching just like what tml does, by which this hack could be wholly migrated to work inside the game process. It's promising. Fortunately I found some code useful from a patcher for 1.3.5.3 and it works greatly.  
However I found several problems on the working principles of patches. For example, we cannot put a single phase to init all patches. Otherwise, what would be expected if a patch is loaded after that init phase of patches? Consistency becomes the fatal problem.  

But still, the tech of injection is useful for the older hack. Running managed code is still promising in an unmanaged cross-process world. So this project is being archived and the older one will be activated.  

At last for anyone who want to make any use of this project: **BE CAREFUL**, some code **WILL NOT** work as expected.
