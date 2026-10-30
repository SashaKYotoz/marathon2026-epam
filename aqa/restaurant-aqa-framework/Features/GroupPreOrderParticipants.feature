@group-preorder @level1
Feature: Group pre-order participants

  The owner of a booking invites guests to a group pre-order. Every guest builds a cart
  of their own, and the owner sees who joined, what each of them chose and what it costs,
  per guest and for the whole group. Each scenario sets up its own location, menu, waiter
  and booking, and removes them again afterwards.

  Background:
    Given a sample restaurant that serves:
      | dish         | price |
      | Caesar Salad | 180   |
      | Tom Yum      | 220   |
      | Cheesecake   | 150   |
    And the owner has booked a table for 4 guests

  Rule: The owner sees every participant, their dishes and the totals

    @ui @smoke
    Scenario: The owner sees what every guest has chosen
      Given Andrii has joined the group and confirmed 2 x Caesar Salad
      And Olena has joined the group with 1 x Tom Yum but has not confirmed yet
      When the owner opens the group pre-order from their reservations
      Then the owner sees these participants:
        | participant | dishes           | total | confirmed |
        | Andrii      | 2 x Caesar Salad | 360   | yes       |
        | Olena       | 1 x Tom Yum      | 220   | no        |
      And the owner sees a group total of 580

    @ui
    Scenario: The group total follows a guest changing their cart
      Given Olena has joined the group with 1 x Tom Yum but has not confirmed yet
      And the owner has opened the group pre-order
      When Olena adds 1 x Cheesecake to their cart
      Then the owner sees a group total of 370

    @ui
    Scenario: A guest who has not chosen anything is still listed
      Given Andrii has joined the group and confirmed 1 x Cheesecake
      And Ivan has joined the group
      When the owner opens the group pre-order
      Then the owner sees these participants:
        | participant | dishes         | total | confirmed |
        | Andrii      | 1 x Cheesecake | 150   | yes       |
        | Ivan        | none           | 0     | no        |
      And the owner sees a group total of 150

    @ui
    Scenario: The owner opens a group nobody has joined yet
      When the owner opens the group pre-order
      Then the owner sees that nobody has joined yet

    @api
    Scenario: Every participant and the group total add up
      Given Andrii has joined the group and confirmed 2 x Caesar Salad, 1 x Tom Yum
      And Olena has joined the group with 1 x Cheesecake but has not confirmed yet
      When the owner looks at the group participants
      Then the owner sees these participants:
        | participant | dishes                        | total | confirmed |
        | Andrii      | 2 x Caesar Salad, 1 x Tom Yum | 580   | yes       |
        | Olena       | 1 x Cheesecake                | 150   | no        |
      And the owner sees a group total of 730

    @api
    Scenario: The total changes when a guest takes a dish out of their cart
      Given Olena has joined the group with 2 x Tom Yum but has not confirmed yet
      When Olena removes 1 x Tom Yum from their cart
      Then the owner sees a group total of 220

    @api
    Scenario: A group nobody has joined has no participants and nothing to pay
      When the owner looks at the group participants
      Then the owner sees that nobody has joined yet

  Rule: Only the owner of the group can see its participants

    @api @negative
    Scenario Outline: Someone other than the owner cannot see the participants
      Given Andrii has joined the group
      When <requester> looks at the group participants
      Then the request is refused because <reason>

      Examples: Everyone but the owner
        | requester                    | reason                          |
        | another customer             | it does not come from the owner |
        | the waiter                   | it does not come from the owner |
        | a guest of the group         | no account is signed in         |
        | someone who is not signed in | no account is signed in         |

    @api @negative
    Scenario: The owner asks for a group that does not exist
      When the owner looks at the participants of a group that does not exist
      Then the owner is told the group pre-order could not be found
