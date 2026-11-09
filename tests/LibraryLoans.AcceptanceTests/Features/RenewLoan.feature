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

  Rule: A loan can be renewed at most twice

    Scenario Outline: Renewal limit
      Given Mia has "Dune" on loan, due 9 November 2026
      And "Dune" has been renewed <renewals> before
      When Mia renews "Dune"
      Then Mia is told "<message>"
      And "Dune" is due on <new due date>

      Examples:
        | renewals | new due date     | message                              |
        | once     | 30 November 2026 | Renewed Dune.                        |
        | twice    | 9 November 2026  | Dune has already been renewed twice. |

  Rule: A loan cannot be renewed while another member has a hold on the title

    Scenario: Another member is waiting
      Given Mia has "Dune" on loan, due 9 November 2026
      And Omar has placed a hold on "Dune"
      When Mia renews "Dune"
      Then Mia is told "Another member is waiting for Dune."
      And "Dune" is due on 9 November 2026

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

  Rule: A member owing more than $10 in fines cannot renew

    Scenario Outline: Fine limit
      Given Mia has "Dune" on loan, due 9 November 2026
      And Mia owes $<fines> in fines
      When Mia renews "Dune"
      Then Mia is told "<message>"
      And "Dune" is due on <new due date>

      Examples:
        | fines | new due date     | message                                                    |
        | 10.00 | 30 November 2026 | Renewed Dune.                                              |
        | 10.01 | 9 November 2026  | You owe $10.01 in fines. Pay your fines before borrowing. |
