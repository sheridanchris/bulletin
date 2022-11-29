module LoginTests

open Login
open Expecto
open Shared

[<Tests>]
let loginTests =
  testList "Login tests" [
    testCaseAsync "User logged in"
    <| async {
      let loginService: LoginService =
        loginService
          (DependencyTypeMocks.findUserByName (fun _ -> Some User.emptyUser)) // user exists
          (DependencyTypeMocks.verifyPasswordHash true) // password matches
          DependencyTypeMocks.signInUser

      let! result =
        loginService
          {
            Username = "username"
            Password = "password"
          }

      return Expect.isOk result "Login result should be InvalidUsernameAndOrPassword"
    }

    testCaseAsync "Invalid username test"
    <| async {
      let loginService: LoginService =
        loginService
          (DependencyTypeMocks.findUserByName (fun _ -> None)) // user does not exist
          (DependencyTypeMocks.verifyPasswordHash false) // password does not match
          DependencyTypeMocks.signInUser

      let! result =
        loginService
          {
            Username = "username"
            Password = "password"
          }

      return
        Expect.isErrorWithPredicate
          (fun value -> value = InvalidUsernameAndOrPassword)
          result
          "Login result should be InvalidUsernameAndOrPassword"
    }

    testCaseAsync "Invalid password test"
    <| async {
      let loginService: LoginService =
        loginService
          (DependencyTypeMocks.findUserByName (fun _ -> Some User.emptyUser)) // user exists
          (DependencyTypeMocks.verifyPasswordHash false) // password does not match
          DependencyTypeMocks.signInUser

      let! result =
        loginService
          {
            Username = "username"
            Password = "password"
          }

      return
        Expect.isErrorWithPredicate
          (fun value -> value = InvalidUsernameAndOrPassword)
          result
          "Login result should be InvalidUsernameAndOrPassword"
    }
  ]
