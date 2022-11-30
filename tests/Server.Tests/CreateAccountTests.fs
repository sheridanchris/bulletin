module CreateAccountTests

open CreateAccount
open Shared
open Expecto

[<Tests>]
let createAccountTests =
  testList "Create account tests" [
    testCaseAsync "Create account successfully"
    <| async {
      let createAccountService: CreateAccountService =
        createAccountService
          (DependencyTypeMocks.findUserByName (fun _ -> None)) // username isn't taken
          (DependencyTypeMocks.findUserByEmailAddress (fun _ -> None)) // email isn't taken
          (DependencyTypeMocks.createPasswordHash "hash")
          DependencyTypeMocks.signInUser
          DependencyTypeMocks.saveUser
          (DependencyTypeMocks.createGravatarUrl "")

      let! result =
        createAccountService
          {
            Username = "username"
            EmailAddress = "email"
            Password = "password"
          }

      return Expect.isOk result "Create account result should be OK"
    }

    testCaseAsync "Username is taken"
    <| async {
      let createAccountService: CreateAccountService =
        createAccountService
          (DependencyTypeMocks.findUserByName (fun _ -> Some User.emptyUser)) // username is taken
          (DependencyTypeMocks.findUserByEmailAddress (fun _ -> None)) // email isn't taken
          (DependencyTypeMocks.createPasswordHash "hash")
          DependencyTypeMocks.signInUser
          DependencyTypeMocks.saveUser
          (DependencyTypeMocks.createGravatarUrl "")

      let! result =
        createAccountService
          {
            Username = "username"
            EmailAddress = "email"
            Password = "password"
          }

      return
        Expect.isErrorWithPredicate
          (fun result -> result = UsernameTaken)
          result
          "Result should be `Error UsernameTaken`"
    }

    testCaseAsync "Email address is taken"
    <| async {
      let createAccountService: CreateAccountService =
        createAccountService
          (DependencyTypeMocks.findUserByName (fun _ -> None)) // username isn't taken
          (DependencyTypeMocks.findUserByEmailAddress (fun _ -> Some User.emptyUser)) // email is taken
          (DependencyTypeMocks.createPasswordHash "hash")
          DependencyTypeMocks.signInUser
          DependencyTypeMocks.saveUser
          (DependencyTypeMocks.createGravatarUrl "")

      let! result =
        createAccountService
          {
            Username = "username"
            EmailAddress = "email"
            Password = "password"
          }

      return
        Expect.isErrorWithPredicate
          (fun result -> result = EmailAddressTaken)
          result
          "Result should be `Error EmailAddressTaken`"
    }
  ]
