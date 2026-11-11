# AI Usage

## Tool and scope

I used Claude (Anthropic) as an assistant across several chats while building this project. It helped with the architecture, backend and frontend code, tests, Docker setup, PowerShell commands, and documentation. The assistant could not read my files or GitHub, so I ran commands and pasted the output back. I made the final decisions and ran every command myself.

## Where the AI made mistakes, and how I handled them

- **Wrong test count.** The assistant said there were 23 tests; the real count was 24. I caught it from the actual `dotnet test` output.
- **Timestamp precision failure.** A test compared `confirmedAt` values, but PostgreSQL stores microseconds while .NET uses ticks, so the comparison failed. The test was fixed to account for the difference.
- **Misleading PowerShell display.** `/me/holds` looked empty in the terminal. I checked with `Invoke-WebRequest` and saw the real response.
- **No access to the repo.** Because the assistant could not read my code, I pasted code snapshots. It sometimes worked from an outdated picture of the files.
- **Guessing before inspecting.** The assistant guessed the wrong database name and a wrong CSS cause for the admin table before looking at the real files.
- **Confusing command order.** Commands for changing `HOLD_DURATION_SECONDS` were ordered badly, which caused a redundant container restart.
- **Duplicate commit message.** A commit command was run twice, which produced a duplicate commit message.
- **API contract checked late.** The assistant did not compare the API against the assignment brief until late. I found the mismatches myself: the class request body, `holdId`, uppercase statuses, 204 on DELETE, the Bengali pay button, and the admin records.
- **Silent regex failures.** A regex rewrite of test literals silently did nothing, and a case-insensitive `Select-String` showed misleading matches. A second regex one-liner on the frontend types also did nothing silently, so I replaced it with a full file rewrite.
- **Unreliable clipboard output.** Copying output through the clipboard was unreliable, so we switched to showing output with `Select-String` and `Get-Content`.
- **Repeated commands and misread output.** The assistant sometimes repeated commands and misread pasted output, especially when I pasted a command without its output. This cost time.
- **README command pasted wrongly.** While writing this README, a here-string containing code fences broke when pasted and left PowerShell stuck at the `>>` prompt. We rebuilt the file in smaller batches using indented code blocks.

## How I verified the AI's work

- I ran the automated tests myself (28 passing) and checked real output instead of trusting claims.
- I used real command output from the running API for the README examples, and left out examples I had not seen.
- I compared the API against the assignment brief and fixed the differences.
- I kept manual browser checks separate from automated tests, and marked what is not verified.
