@group-preorder @level2
Feature: Group pre-order confirmation

  After looking at what everyone chose, the owner confirms one combined pre-order and it
  goes to the restaurant. Only confirmed carts are taken, identical dishes are combined
  into one line, the combined dishes join the reservation's pre-order and the waiter
  sees them. The owner also has a cart of their own in the group.

  Background:
    Given a sample restaurant that serves:
      | dish         | price |
      | Caesar Salad | 180   |
      | Tom Yum      | 220   |
      | Cheesecake   | 150   |
    And the owner has booked a table for 4 guests

  Rule: Confirming sends one combined pre-order to the restaurant

    Background:
      Given the owner has already pre-ordered 1 x Cheesecake for the reservation

    @ui @smoke
    Scenario: Identical dishes are combined and unconfirmed carts are left out
      Given Andrii has joined the group and confirmed 2 x Caesar Salad, 1 x Tom Yum
      And Olena has joined the group and confirmed 1 x Caesar Salad
      And Ivan has joined the group with 3 x Tom Yum but has not confirmed yet
      And the owner has opened the group pre-order
      When the owner confirms the group pre-order
      Then the pre-order is sent to the restaurant with:
        | dish         | quantity |
        | Caesar Salad | 3        |
        | Tom Yum      | 1        |

    @ui
    Scenario: The owner adds dishes of their own before confirming
      Given Andrii has joined the group and confirmed 1 x Caesar Salad
      And the owner has opened the group pre-order
      When the owner chooses 2 x Tom Yum for themselves and confirms them
      And the owner confirms the group pre-order
      Then the pre-order is sent to the restaurant with:
        | dish         | quantity |
        | Caesar Salad | 1        |
        | Tom Yum      | 2        |

    @ui
    Scenario: The waiter sees what the owner confirmed
      Given Andrii has joined the group and confirmed 2 x Caesar Salad
      And Olena has joined the group and confirmed 1 x Caesar Salad, 1 x Tom Yum
      And the owner has confirmed the group pre-order
      When the waiter opens the pre-order of the reservation
      Then the waiter sees the pre-order:
        | dish         | quantity |
        | Caesar Salad | 3        |
        | Tom Yum      | 1        |
        | Cheesecake   | 1        |

    @api
    Scenario: The restaurant receives the combined pre-order
      Given Andrii has joined the group and confirmed 2 x Caesar Salad
      And Olena has joined the group and confirmed 1 x Caesar Salad, 1 x Tom Yum
      And Ivan has joined the group with 1 x Cheesecake but has not confirmed yet
      When the owner confirms the group pre-order
      Then the pre-order is sent to the restaurant with:
        | dish         | quantity |
        | Caesar Salad | 3        |
        | Tom Yum      | 1        |
      And the restaurant has received the group pre-order
      And the waiter sees the pre-order:
        | dish         | quantity |
        | Caesar Salad | 3        |
        | Tom Yum      | 1        |
        | Cheesecake   | 1        |

    @api
    Scenario: The owner's own confirmed dishes are part of the group pre-order
      Given the owner has confirmed 1 x Tom Yum for themselves
      And Andrii has joined the group and confirmed 1 x Caesar Salad
      When the owner confirms the group pre-order
      Then the pre-order is sent to the restaurant with:
        | dish         | quantity |
        | Caesar Salad | 1        |
        | Tom Yum      | 1        |

  Rule: A group pre-order cannot be confirmed while there is nothing to confirm

    Background:
      Given the owner has already pre-ordered 1 x Cheesecake for the reservation

    @ui @negative
    Scenario: The owner is told on the page why nothing can be confirmed yet
      Given Andrii has joined the group with 1 x Tom Yum but has not confirmed yet
      And the owner has opened the group pre-order
      When the owner confirms the group pre-order
      Then the owner is told "There are no submitted guest sessions to confirm."

    @api @negative
    Scenario: No guest has confirmed their cart yet
      Given Andrii has joined the group with 1 x Tom Yum but has not confirmed yet
      When the owner confirms the group pre-order
      Then the owner is told "There are no submitted guest sessions to confirm."

    @api @negative
    Scenario: A cart without dishes cannot be confirmed in the first place
      Given Andrii has joined the group
      When Andrii tries to confirm their empty cart
      Then the guest is told their cart is empty

  Rule: The group pre-order joins the reservation's own pre-order

    @api @negative
    Scenario: The reservation has no pre-order to add the group's dishes to
      Given Andrii has joined the group and confirmed 1 x Caesar Salad
      When the owner confirms the group pre-order
      Then the owner is told "Pre-order for this reservation was not found."

  Rule: Only the owner can confirm, and only an open group pre-order

    Background:
      Given the owner has already pre-ordered 1 x Cheesecake for the reservation
      And Andrii has joined the group and confirmed 1 x Caesar Salad

    @api @negative
    Scenario: The owner confirms a group that does not exist
      When the owner confirms a group pre-order that does not exist
      Then the owner is told the group pre-order could not be found

    @api @negative
    Scenario: The owner confirms for a reservation that does not exist
      When the owner confirms the group pre-order for a reservation that does not exist
      Then the owner is told the group pre-order could not be found

    @api @negative
    Scenario: A group pre-order cannot be confirmed twice
      Given the owner has confirmed the group pre-order
      When the owner confirms the group pre-order
      Then the owner is told the group pre-order was already confirmed

    @api @negative
    Scenario Outline: Someone other than the owner cannot confirm
      When <requester> confirms the group pre-order
      Then the request is refused because <reason>

      Examples: Everyone but the owner
        | requester                    | reason                          |
        | another customer             | it does not come from the owner |
        | the waiter                   | it does not come from the owner |
        | a guest of the group         | no account is signed in         |
        | someone who is not signed in | no account is signed in         |

  Rule: The owner has a cart of their own in the group

    @api
    Scenario: The owner opens their own cart
      When the owner opens their own cart in the group
      Then the owner gets their own cart in the group

    @api @negative
    Scenario Outline: Nobody else has the owner's cart
      When <requester> opens their own cart in the group
      Then the request is refused because <reason>

      Examples: Everyone but the owner
        | requester                    | reason                          |
        | another customer             | it does not come from the owner |
        | the waiter                   | it does not come from the owner |
        | someone who is not signed in | no account is signed in         |
