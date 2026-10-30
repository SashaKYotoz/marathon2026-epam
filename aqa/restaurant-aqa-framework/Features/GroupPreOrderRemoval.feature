@group-preorder @level3
Feature: Removing a participant from a group pre-order

  The owner can take a participant out of the group pre-order and immediately sees the
  remaining participants and the new total. Only the owner can do it, and only for
  someone who really is a participant of that very group.

  Background:
    Given a sample restaurant that serves:
      | dish         | price |
      | Caesar Salad | 180   |
      | Tom Yum      | 220   |
      | Cheesecake   | 150   |
    And the owner has booked a table for 4 guests

  Rule: The owner removes a participant and sees the group without them

    @ui @smoke
    Scenario: The owner removes one of several participants
      Given Andrii has joined the group and confirmed 2 x Caesar Salad
      And Olena has joined the group with 1 x Tom Yum but has not confirmed yet
      And the owner has opened the group pre-order
      When the owner removes Andrii from the group
      Then the owner sees these participants:
        | participant | dishes      | total | confirmed |
        | Olena       | 1 x Tom Yum | 220   | no        |
      And the owner sees a group total of 220

    @api
    Scenario: The remaining participants come back right after a removal
      Given Andrii has joined the group and confirmed 2 x Caesar Salad
      And Olena has joined the group with 1 x Tom Yum but has not confirmed yet
      When the owner removes Olena from the group
      Then the owner gets back these participants:
        | participant | dishes           | total | confirmed |
        | Andrii      | 2 x Caesar Salad | 360   | yes       |
      And the owner gets back a group total of 360

    @api
    Scenario: A participant who already confirmed their cart can be removed
      Given Andrii has joined the group and confirmed 2 x Caesar Salad
      And Olena has joined the group and confirmed 1 x Tom Yum
      When the owner removes Andrii from the group
      Then the owner gets back these participants:
        | participant | dishes      | total | confirmed |
        | Olena       | 1 x Tom Yum | 220   | yes       |
      And Andrii is no longer in the group

    @api
    Scenario: Removing the last participant leaves an empty group
      Given Andrii has joined the group and confirmed 1 x Cheesecake
      When the owner removes Andrii from the group
      Then the owner gets back an empty group

    @api
    Scenario: A removed participant's dishes are not part of the confirmed pre-order
      Given the owner has already pre-ordered 1 x Cheesecake for the reservation
      And Andrii has joined the group and confirmed 2 x Caesar Salad
      And Olena has joined the group and confirmed 1 x Tom Yum
      And the owner has removed Andrii from the group
      When the owner confirms the group pre-order
      Then the pre-order is sent to the restaurant with:
        | dish    | quantity |
        | Tom Yum | 1        |

  Rule: Only the owner can remove, and only a participant of this group

    @api @negative
    Scenario: A participant of another group cannot be removed from this one
      Given Andrii has joined the group
      And Taras has joined another group of the owner
      When the owner removes Taras from the group
      Then the owner is told the participant is not part of this group

    @api @negative
    Scenario: The owner removes a participant who does not exist
      When the owner removes a participant who does not exist
      Then the owner is told the participant could not be found

    @api @negative
    Scenario Outline: Someone other than the owner cannot remove a participant
      Given Andrii has joined the group
      When <requester> removes Andrii from the group
      Then the request is refused because <reason>
      And Andrii is still in the group

      Examples: Everyone but the owner
        | requester                    | reason                          |
        | another customer             | it does not come from the owner |
        | the waiter                   | it does not come from the owner |
        | a guest of the group         | no account is signed in         |
        | someone who is not signed in | no account is signed in         |

  Rule: The whole journey from the guests to the waiter

    @ui @e2e
    Scenario: Guests order, the owner removes one of them and confirms, and the waiter sees the rest
      Given the owner has already pre-ordered 1 x Cheesecake for the reservation
      And Andrii has joined the group and confirmed 2 x Caesar Salad
      And Olena has joined the group and confirmed 1 x Tom Yum
      And Ivan has joined the group and confirmed 1 x Tom Yum, 1 x Caesar Salad
      When the owner opens the group pre-order from their reservations
      Then the owner sees a group total of 980
      When the owner removes Ivan from the group
      Then the owner sees a group total of 580
      When the owner confirms the group pre-order
      Then the pre-order is sent to the restaurant with:
        | dish         | quantity |
        | Caesar Salad | 2        |
        | Tom Yum      | 1        |
      When the waiter opens the pre-order of the reservation
      Then the waiter sees the pre-order:
        | dish         | quantity |
        | Caesar Salad | 2        |
        | Tom Yum      | 1        |
        | Cheesecake   | 1        |
