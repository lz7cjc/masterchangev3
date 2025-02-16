<?php
require("dbconnect.php");

//from Unity 
/*$userid = $_POST["user"];
$dbstring = $_POST["cthearray"];*/


//pasted from unity 
$userid = 787;
$unityInput = '{"data1":[{"Habit_ID":35,"label":"binary","amount":0},{"Habit_ID":34,"label":"binary","amount":0},{"Habit_ID":33,"label":"binary","amount":0}]}';

$decoded = json_decode($unityInput, true);
echo "jsoN looks like: " . $decoded;
//Hardcoded
/*$userid ="456";

$dbstring = array(
array(
  'habitid' => 35,
  'Entry' => 1, 
  'which' =>'binary'
  ),
  array(
  'habitid' => 34,
  'Entry' => 1,
  'which' =>'binary'
  ),
  array(
  'habitid' => 33,
  'Entry' => 45,
  'which' =>'amount'
  ),
  array(
  'habitid' => 32,
  'Entry' => 0,
  'which' =>'binary'
  ),
  array(
  'habitid' => 29,
  'Entry' => 1,
  'which' =>'binary'
  ),
  array(
  'habitid' => 30,
  'Entry' => 16,
  'which' =>'amount'
  ),
  array(
  'habitid' => 31,
  'Entry' => 0,
  'which' =>'binary'
  )
);*/

//echo "the array is: " . $dbstring . "<br>"; 
foreach ( $dbstring as $line) 
  {

     foreach ($line as $key => $value ) 
     {
     // echo "<br>1<br>The Type is: <b>" .$key . "</b> and the value is: <b> " . $value . "</b><br>"; 
          if ($key == "habitid")
          {
      //          echo "2a: first if statement: " . $key . "<br>";
                $sthabitid = $value;
     //           echo "variable sthabitid:  " . $sthabitid . "<br>";
          }
          elseif ($key == "amount")
          {
      //          echo "2b: second if statement: " . $key . "<br>";
                 $stvalue = $value;
     //           echo "variable stvalue:  " . $stvalue . "<br>";
 
          }
          elseif ($key == "label")
          {
      //          echo "2c: third if statement: " . $key . "<br>";
                 $stwhich = $value;
      //          echo "variable sttype:  " . $stwhich . "<br>";
 
          }

      }
      //check if record already exists
    $checkquery =  "SELECT * from userhabits where habit_id ='" . $sthabitid . "' and user_id = '" . $userid . "';"; 
    //echo "1. first query to see any records exist" . $checkquery . "<br>";
    $runCheck = mysqli_query($conn, $checkquery) or die("2:check query failed");

//if there is a record for that habit and that user, update
        if(mysqli_num_rows($runCheck) >0)
            {
	      //  echo "3: habit already exists";
    
            //if boolean, update
            if ($stwhich == "binary")
                {
	            $updatevalue = "UPDATE userhabits SET yesorno = '" . $stvalue . "' WHERE user_id = '" . $userid . "' AND habit_id = '" . $sthabitid . "';"; 
           //     echo "2a. Binary statement for update: " . $updatevalue . "<br>";

      mysqli_query($conn, $updatevalue) or die("4: update failed "); //insert data or error for failure
                }
            elseif ($stwhich == "amount")
                {
	            $updatebool = "UPDATE userhabits SET amount = '" . $stvalue . "' WHERE user_id = '" . $userid . "' AND habit_id = '" . $sthabitid . "';"; 
          //      echo "2b: values statement for update: " . $updatebool . "<br>";

       mysqli_query($conn, $updatebool) or die("4: update failed "); //insert data or error for failure
                }
                 
 
            }
//if there is no record then insert
        else
            {
	         echo "3: new record";
                if ($stwhich == "binary")
                    {
	                $inserthabitbinary = "INSERT INTO userhabits (user_id, habit_id, yesorno) VALUES ('" . $userid . "','" . $sthabitid . "','" . $stvalue .  "');";
	         //       echo "3a insert binary values: " . $inserthabitbinary . "<br>";
                   mysqli_query($conn, $inserthabitbinary) or die("4: insert failed"); //insert data or error for failure
        
                    }
                elseif ($stwhich == "amount")
                    {
	                $inserthabitamount = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $sthabitid . "','" . $stvalue .  "');";
             //       echo "3b: insert amount values:" . $inserthabitamount . "<br>";
                     mysqli_query($conn, $inserthabitamount) or die("4: insert failed"); //insert data or error for failure
        

                    }
	           }



	//echo "<br> ---------------------------------<br>End of insert/update for #age <br> ---------------------------------<br>";

    
 //echo  "<br>end of this inside loop <br><br>";
}  

$conn->close();

?>
