# ADR-010 — No XML, No JSON, No YAML: Pure C# API
# Arora.Workflow

**Date**: 2026-07-01
**Status**: Accepted

---

## Context

Many workflow platforms define workflows in XML (BPMN), JSON (Elsa's import/export format), or YAML (GitHub Actions, Temporal workflows in some SDKs). This separates workflow "configuration" from code.

Arora.Workflow must decide whether workflow definitions are code or configuration.

---

## Options Considered

| Option | Pros | Cons |
|--------|------|------|
| XML / BPMN | Industry standard, toolable | Verbose, not type-safe, requires XML tooling, not testable without a parser |
| JSON / YAML | Human-readable, storable in DB | Not type-safe, no IntelliSense, no refactoring support, not unit-testable |
| Pure C# fluent API | Type-safe, IntelliSense, refactorable, unit-testable | Requires code changes to modify a definition |
| Hybrid (code + JSON export) | Flexibility | Two APIs to maintain; JSON format must stay in sync with code API |

---

## Decision

**Pure C# fluent API. No XML. No JSON definition format. No YAML.**

Workflow definitions are C# code. They are compiled. They have IntelliSense. They are type-safe. They are refactorable. They are unit-testable.

---

## Why

1. **The Manifesto**: *"Workflow should be understandable."* A C# workflow definition is readable in a code review. An XML definition is not. A JSON definition is barely readable. A YAML definition requires understanding a proprietary schema.
2. **Type safety**: `IWorkflowStep<InvoiceInput, ValidationResult>` is type-safe. `"step": "ValidateInvoiceStep"` is a magic string. Magic strings break at runtime. Type-safe code breaks at compile time.
3. **IntelliSense and refactoring**: When a developer renames `ValidateInvoiceStep` to `InvoiceValidationStep`, the compiler and IDE rename every reference automatically. A JSON definition file silently breaks.
4. **Unit-testable definitions**: A C# `WorkflowDefinition` can be instantiated in a unit test and validated with `WorkflowDefinitionValidator`. A JSON file requires parsing, deserialization, and validation in a more complex pipeline.
5. **Target persona alignment**: The primary persona is a .NET developer. They write C#. Making them write JSON to define a workflow is a regression in developer experience.
6. **The EF Core analogy**: Entity Framework Core does not define database schemas in XML. It defines them in C# with `ModelBuilder`. This is the established .NET pattern for code-first definitions of complex structures.

---

## Trade-offs

- Business analysts who cannot write C# cannot modify workflow definitions without developer involvement. This is a deliberate limitation for v1. The visual designer (Phase 5) addresses this for the non-developer persona.
- Workflow definitions cannot be stored in a database and loaded at runtime without a serialization format. `DefinitionJson` in the database stores the serialized graph — but this is an *internal* representation, not a public API. It is not stable across versions.
- Hotloading workflow definitions without redeploying the application is not supported in Phase 1 (requires code recompilation). Phase 3+ may introduce `IWorkflowDefinitionSource` implementations that support dynamic loading.

---

## Consequences

- The public definition API is exclusively the fluent C# builder (`WorkflowDefinition.Create(...)...Build()`).
- No public JSON schema for workflow definitions exists or is planned for Phase 1 or 2.
- The `DefinitionJson` column in `WorkflowDefinitions` stores an internal, versioned serialization for the engine's own use. It is not consumed by host applications.
- The Roslyn analyzer enforces that step names in definitions reference only registered step types (`AW002: Step type not registered`).
