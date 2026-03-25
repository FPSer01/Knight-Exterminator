========== Pamuk AI ==========

PamukAI (PAI) is an extremely lightweight, low-level AI system designed for simple imperative code and maximum performance. It is not a framework, but a flexible code-first library that allows  you to build AI systems using Behavior Trees, FSMs, Utility AI and Flow-like logic — in any combination and all in a unified and consistent style.

========== Fast Start ==========

The main thing you need to understand to use the library is the structure of the Method. It should be like this:

bool Foo()
{
    if (Step()) { ...; if (condition_of_step0) return true; }
    DoSomethingAfterStep0();
    if (DoOnce()) { ... }
    if (Wait(2)) return true;
    if (Step()) { ... }
    return true/false;
}

Key points:
The block inside Step should return true and then the step will continue, and not return anything (i.e. not call return) to go to the next step.
Returning false from anywhere stops Method execution and reset its state to Step0.
Do not use Step, Wait, DoOnce, OnExit and Wait inside each other.
You can write code outside of Step blocks. Using Step is not required at all.

You will find all the helper methods in the PAI static class, their purpose is easy to understand from the name. Demo examples give an idea of ​​​​how everything works. 

========= Links ===========
Discord: https://discord.gg/YNs7uTTvaa
Documentation: https://docs.google.com/document/d/1w_rURLQYAgKpgYA1HnZSTh-SiHD08LdKzluA8Dh8ZjE/edit?usp=sharing