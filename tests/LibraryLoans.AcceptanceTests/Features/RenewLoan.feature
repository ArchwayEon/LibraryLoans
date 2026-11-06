Feature: Renew a loan (LL-12)
  As a member, I want to renew a book I have on loan,
  so that I can keep reading it without visiting the library.

  Background:
    Given today is 2 November 2026
    And Mia is a member

  Rule: A renewal extends the due date by 21 days from the current due date

    Scenario: Renewing moves the due date three weeks later
      Given Mia has "Dune" on loan, due 9 November 2026
      When Mia renews "Dune"
      Then Mia is told "Renewed Dune."
      And "Dune" is due on 30 November 2026

  Rule: An overdue loan cannot be renewed

    Scenario Outline: Overdue boundary
      Given Mia has "Dune" on loan, due <due date>
      When Mia renews "Dune"
      Then Mia is told "<message>"
      And "Dune" is due on <new due date>

      Examples:
        | due date        | new due date     | message                          |
        | 2 November 2026 | 23 November 2026 | Renewed Dune.                    |
        | 1 November 2026 | 1 November 2026  | Overdue loans cannot be renewed. |
