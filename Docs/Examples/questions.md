# C# Memory Management Notes

Some personal notes about memory in C#.

The CLR manages memory automatically using the Garbage Collector.

---

:::rc-question

type: Recall

concepts:
- Boxing
- Value Types
- Reference Types

:::rc-prompt

What is boxing in C#?

:::

:::rc-answer

Boxing is the process of converting a value type into an object or interface type.

The runtime creates an object on the managed heap and copies the value into that object.

:::

:::


---

More notes about garbage collection.

---

:::rc-question

type: Explanation

concepts:
- Garbage Collection
- CLR
- Memory Management

:::rc-prompt

Explain how garbage collection works in .NET.

Your answer should include generations and why they exist.

:::

:::rc-answer

The .NET Garbage Collector automatically manages memory by identifying objects that are no longer reachable and reclaiming their memory.

Objects are organized into generations based on their lifetime. New objects start in Generation 0, and objects that survive collections are promoted to higher generations.

Generational collection improves performance because most objects are short-lived.

:::

:::


---

:::rc-question

type: Synthesis

concepts:
- Garbage Collection
- Memory Allocation
- Performance Optimization

:::rc-prompt

How can poor object allocation patterns negatively affect application performance in a managed language like C#?

:::

:::
