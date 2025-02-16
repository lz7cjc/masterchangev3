<?php
require("dbconnect.php");

//from Unity 
$userid = $_POST["user"];
$json = $_POST["cthearray"];


//pasted from unity 
 /*$userid = "16589459";
 $json = '{"data1":[{"Habit_ID":27,"label":"slider","amount":127},{"Habit_ID":26,"label":"slider","amount":67},{"Habit_ID":28,"label":"slider","amount":210}]}';*/

$decoded = json_decode($json, true);



    foreach($decoded["data1"] as $habit) 
    {
    /*    echo "<br>loop starts<br>ID: " . $habit["Habit_ID"] . "<br>";
        echo "<br>label: " . $habit["label"] . "<br>";
        echo "<br>amount: " . $habit["amount"] . "<br> loop ends";
  */     
    
      //check if record already exists
      $checkquery =  "SELECT * from userhabits where habit_id ='" . $habit["Habit_ID"]  . "' and user_id = '" . $userid . "';"; 
      //  echo "1 check. first query to see any records exist" . $checkquery . "<br>";
      $runCheck = mysqli_query($conn, $checkquery) or die("2:check query failed");
//echo "runcheck sql: " . $checkquery;
    /*echo "<br>wwhat do i get as label value for if statement: " . $habit["label"] . "<br>";
    *///check if a value or yesno
            if ($habit["label"] == "toggle")
            {
               
           
                //if there is a record for that habit and that user, update
                if(mysqli_num_rows($runCheck) >0)
                    {
             //     echo "1: label already exists"; 
                  $reward = "0";
                  $updatevalue = "UPDATE userhabits SET yesorno = '" . $habit["amount"] . "' WHERE user_id = '" . $userid . "' AND habit_id = '" . $habit["Habit_ID"] . "';"; 
                //        echo "2a. Binary statement for update: " . $updatevalue . "<br>";

                  mysqli_query($conn, $updatevalue) or die("1xxx: update failed "); //insert data or error for failure
                
                    }
                //if there is no record then insert
                else
                    {
                //     echo "<br>2 binary: new record";
                      $reward = "1";
                      $inserthabitbinary = "INSERT INTO userhabits (user_id, habit_id, yesorno) VALUES ('" . $userid . "','" . $habit["Habit_ID"]  . "','" . $habit["amount"]  .  "');";
        //         echo "3a insert binary values: " . $inserthabitbinary . "<br>";
                   mysqli_query($conn, $inserthabitbinary) or die("4: insert failed"); //insert data or error for failure
                     
                    }

            }

    else if ($habit["label"] == "slider")
        
            {
              /*  echo "value if statement";
              */  //if there is a record for that habit and that user, update
              if(mysqli_num_rows($runCheck) >0)
                  {
                    $reward = "0";
             //   echo "<br>3: habit already exists";
          
                $updatevalue = "UPDATE userhabits SET amount = '" . $habit["amount"] . "' WHERE user_id = '" . $userid . "' AND habit_id = '" . $habit["Habit_ID"] . "';"; 
               /*       echo "2a. Binary statement for update: " . $updatevalue . "<br>";
                */
              mysqli_query($conn, $updatevalue) or die("1xxx: update failed "); //insert data or error for failure
              
         
            }
//if there is no record then insert
        else
            {
             //echo "<br>4: new record";
             $reward = "1";
             $inserthabitbinary = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $habit["Habit_ID"]  . "','" . $habit["amount"]  .  "');";
                // echo "4 insert binary values: " . $inserthabitbinary . "<br>";
                   mysqli_query($conn, $inserthabitbinary) or die("4: insert failed"); //insert data or error for failure
             
            }

          }
}
// echo "get in back" . $newhabits; 
echo '{"reward":' . $reward . '}';

$conn->close();

?>
