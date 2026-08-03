# SOLID

SOLID is a set of five widely used object-oriented design principles. They are guidelines for keeping responsibilities, contracts, and dependency directions understandable; they are not a requirement to create abstractions where the problem does not need them.

## Principles

- Single Responsibility Principle: a class or module should have one responsibility and one reason to change. Keep unrelated concerns separate.
- Open/Closed Principle: software entities should be open for extension and closed for modification. Extend stable behavior through an appropriate contract when variation is real instead of repeatedly changing the same dispatch code.
- Liskov Substitution Principle: objects of a subtype must be usable wherever the base type is expected without breaking the base contract. Subtypes must preserve the guarantees and valid use cases of their abstractions.
- Interface Segregation Principle: clients should not be forced to depend on methods they do not use. Prefer focused interfaces shaped around actual client needs.
- Dependency Inversion Principle: high-level policy should not depend directly on low-level details; both should depend on abstractions, and details should depend on those abstractions. Keep dependency direction explicit and construction in the composition boundary.

Apply SOLID together with the requirements and KISS. Do not split a cohesive type, introduce an interface, or add an abstraction solely to satisfy a letter of the acronym.
