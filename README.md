# Gesture-Recognition-WPF-Server-Side
Server-Side written with c# WPF, server for android client-side

# #Techs
- Native WPF to build the UI
- Storyboard with DoubleAnimation to build the Projector Animation
- Using `TWO` storyboard to build the closing and the opening animation.
- Using [Eneter](http://www.eneter.net/) to build the `TCP channal` between `.NET server` and `Android client`  
  Eneter is a fantastic framework for msg communication. *BUT* `Android 7.0` NOT SUPPORT! I can not use it in my  
  Nexues 6 with Android 7.0.1. More develope details on this communication procedure, please go this [Blog](https://www.codeproject.com/articles/340714/android-how-to-communicate-with-net-application-vi). It is GOOD!
- Using FSM(finite state machine) to handle Multi State Transfer.
- Using Child Thread to update WPF Main Thread UI changes. More details need to have a look at this [Blog](http://www.cnblogs.com/chenxizhang/archive/2010/03/25/1694604.html).
