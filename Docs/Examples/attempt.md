---
type: assessment
created: 2026-07-13T20:15:00
assessment: assessment-csharp-memory-001
---

# C# Memory Management Assessment

Answer the following questions in your own words.

Focus on understanding concepts rather than memorizing definitions.

---

## Question 1

What is boxing in C#?

### Answer

Boxing is when a value type is converted into an object.

For example, an integer can be stored inside an object variable.

The runtime creates an object and copies the value into it.

---

## Question 2

Explain how garbage collection works in .NET.

Your answer should include generations and why they exist.

### Answer

The garbage collector automatically removes unused objects from memory.

Objects are placed into generations. New objects are generation 0, and objects that survive move to higher generations.

The reason for this is optimization because most objects die quickly.

---

## Question 3

How can poor object allocation patterns negatively affect application performance in a managed language like C#?

### Answer

Creating many objects can make the garbage collector run more often.

This can slow down the application because more memory cleanup is required.
