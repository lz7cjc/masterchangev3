<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "lz7cj-2go-u-259938";*/

//remote machine
$servername = "10.16.16.15";
$username = "lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "lz7cj-2go-u-259938";


//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

//check connection
if ($conn->connect_error) 
{

	die("Connection failed " . $conn->connect_error);

}

//store information entered in form as variables to insert into DB
$contentid = $_POST['contentid'];
$habitid = $_POST['habits'];
$minvalue = $_POST['minvalue'];
//echo "min valie is what: " . $minvalue;
if (empty($minvalue))
{ 
$minvalue = 'null'; 
echo "min valie is what: " . $minvalue;

}
else
{
  $minvalue = "'" . $minvalue . "'";
 // echo "min valie is what:11" . $minvalue;

}
$maxvalue = $_POST['maxvalue'];
if (empty($maxvalue))
{ 
$maxvalue = 'null'; 
//echo "maxvalue valie is what: " . $maxvalue;

}
else
{
  $maxvalue = "'" . $maxvalue . "'";
 // echo "maxvalue valie is what:11" . $maxvalue;

}

$boolvalue = $_POST['yesorno'];

if (empty($boolvalue))
{ 
$boolvalue = 'null'; 
//echo "boolvalue valie is what: " . $boolvalue;

}
else
{
  $boolvalue = "'" . $boolvalue . "'";
  //echo "boolvalue valie is what:11" . $boolvalue;

}

$label = $_POST['label'];
$description = $_POST['description'];
$generic = $_POST['everywhere'];
if (empty($generic))
{ 
$generic = 'null'; 
//echo "generic valie is what: " . $generic;

}
else
{
  $generic = "'" . $generic . "'";
  //echo "generic valie is what:11" . $generic;

}
$ContentTitle = $_POST['ContentTitle'];
$ContentBody = $_POST['ContentBody'];/////////////////////////////////////////////
//Insert the values posted to this page 
/////////////////////////////////////////////
//////////insert behaviours
if (is_null($habitid))
{
	//echo "no habits to insert" . $habitid;
}
else
{
$inserthabitparameters = "INSERT INTO content_habitparameter (Content_ID, Habit_ID, Min_Value, Max_Value, yesorno, Section_Generic) VALUES ('" . $contentid . "','" .  $habitid . "'," . $minvalue . "," . $maxvalue . "," . $boolvalue . "," . $generic . ")";

//echo $inserthabitparameters;
mysqli_query($conn, $inserthabitparameters) or die("4: failed to insert habits"); //insert data or error for failure

//echo $inserthabitparameters . "<br>";
}







//echo $behaviours[];
//echo $title . "<br>" . $description;


/*
        // Check if any option is selected 
        if(isset($_POST["habits"]))  
        	print "Habit IDs <br>";

        { 
            // Retrieving each selected option 
            foreach ($_POST['habits'] as $habit)  
                print "You selected $habit<br/>"; 
        } 
    
        // Check if any option is selected 
        if(isset($_POST["behaviours"]))  
        { 
        	print "behaviours IDs <br>";
        
            // Retrieving each selected option 
            foreach ($_POST['behaviours'] as $behaviour)  
                print "You selected $behaviour<br/>"; 
        } 
    
     */



//values into db
/*$insertcontent = "INSERT INTO contents (ContentTitle, ContentBody, ContentType, Generic) VALUES ('" . $title . "','" . $description . "','" . $ContentType . "','" . $wheretogo . "')";


mysqli_query($conn, $insertcontent) or die("4: failed to insert contents"); //insert data or error for failure
	
	$getcontentid = "Select Content_ID from contents where ContentTitle ='" . $title . "'";

$result = $conn->query($getcontentid);

if ($result->num_rows > 0) {
  // output data of each row
  while($row = $result->fetch_assoc()) {
  	$newContentID = $row["Content_ID"];
   //  echo "content id: " . $row["Content_ID"];
   // echo "newContentID id: " . $newContentID;
  }
} else {
  echo "0 results";
}*/

////////////////////////////////////////////////////////
////Get values to populate the drop down lists
////////////////////////////////////////////////////////
//get habits
$sql2= "SELECT Habit_ID, label, description FROM habits";
 
//Prepare the select statement.
$result2 = mysqli_query($conn, $sql2) or die("2:name check query failed");

if ($result2->num_rows > 0) 
{
   $dbdata2 = array();
}
while ($row = $result2->fetch_assoc())
{
$dbdata2[]=$row;

}

//get behavious types options
$sql3= "SELECT BehaviourType_ID	, Behaviour_Type, Behaviour_Description FROM behaviour_type";
 
//Prepare the select statement.
$result3 = mysqli_query($conn, $sql3) or die("2:name check query failed");

if ($result3->num_rows > 0) 
{
   $dbdata3 = array();
}
while ($row = $result3->fetch_assoc())
{
$dbdata3[]=$row;

}
//var_dump($dbdata1);
/*
$inserthabitparameters = "INSERT INTO content_habitparameters (Content_ID, Habit_ID, Min_Value, Max_Value, 'yesorno', Section_Generic) VALUES ('" . $contentid . "','" .  $habitid . "','" . $minvalue . "','" . $maxvalue . "','" . $boolvalue . "','" . $generic . "')";
echo $inserthabitparameters;*/
?>
<!-- <br>
<form  action="behavioursentry.php" method="post">
<input type="text" name = "habitid" value="habitid"> 
<input type="text" name = "minvalue" value="minvalue">  <input type="text" name = "maxvalue" value="maxvalue">   <input type="text" name = "boolvalue" value="boolean1 or 0"> <input type="text" name = "generic" value="can this go anywhere? 1 or 0"> 
<br>
<input type="text" name = "behaviourid" value="behaviourid" > 
<input type="hidden" id="contentid" name="contentid" value="<?= $newContentID;?>">
 <input type="submit" name="button" value="Submit">
</form> 
<br>
<table border="1"> 
<tr>
	<td>
    <?php foreach($dbdata2 as $item2): 
        echo "<tr><td>" . $item2['Habit_ID'] ."</td><td>" . $item2['label'] . "</td><td>" . $item2['description'] . "</td></tr>"; 
    	endforeach; ?>
</td>

	<td>

<?php foreach($dbdata3 as $item3): 
        echo "<tr><td>" . $item3['BehaviourType_ID'] ."</td><td>" . $item3['Behaviour_Type'] . "</td><td>" . $item3['Behaviour_Description'] . "</td></tr> "; 
    	endforeach; ?>


	</td>
</tr>

</table>
<form action="contententry.php" method="post">

     Habits<br> </select>
<select name="where">

    <?php foreach($dbdata1 as $item1): ?>
        <option value="<?= $item1['generic_optionsid']; ?>"><?= $item1['label']; ?></option>
    <?php endforeach; ?>

</select>
<br>
<select name='habits[]' multiple="multiple" size=60>
    <?php foreach($dbdata2 as $item2): ?>
        <option value="<?= $item2['Habit_ID']  ; ?>"><?= $item2['label'] . "---" . $item2['description']; ?></option>
    <?php endforeach; ?>
</select>
  <br>
  Behaviours
  <br>
<select name='behaviours[]' multiple="multiple" size=16>
    <?php foreach($dbdata3 as $item3): ?>
        <option value="<?= $item3['BehaviourType_ID']  ; ?>"><?= $item3['Behaviour_Type'] . "---" . $item3['Behaviour_Description']; ?></option>
   <?php endforeach; ?>  </select>
 <input type="text" name = "minvalue" value="minvalue">  <input type="text" name = "maxvalue" value="maxvalue">   <input type="text" name = "yesorno" value="boolean1 or 0"> 


 <input type="submit" name="button" value="Submit">
 -->
<!-- <form action="habitsentry.php" method="post">

 <table>
  <tr>
    <td>
    	Add Habits
    	
<select name='habits' size=5>
	 <?php foreach($dbdata2 as $item2): ?>
        <option value="<?= $item2['Habit_ID']  ; ?>"><?= $item2['label'] . "---" . $item2['description']; ?></option>
    <?php endforeach; ?>
</select>
</td>
<td>
minvalue<br>
<input type="text" name = "minvalue" value="">  
</td>
<td>
  maxvalue<br><input type="text" name = "maxvalue" value=""> 
  </td>
  <td>Display if yes or no<br><select name="yesorno">
   <option value="">Irrelevant</option>
  <option value=1>Yes</option>
  <option value=0>No</option>
   </select>
</td>
  <td> Generic or start up <br><select name="generic">
    <option value=1>Startup</option>
   <option value=2>Everywhere</option>
  </select>
</td>

</tr>
</table>
 <input type="hidden" id="contentid" name="contentid" value="<?= $contentid;?>">
  <input type="hidden" id="title" name="title" value="<?= $title;?>">
   <input type="hidden" id="description" name="description" value="<?= $description; ?>">
 <br>
  Behaviours
  
  <br>
<select name='behaviours'  size=5>
		
    <?php foreach($dbdata3 as $item3): ?>
        <option value="<?= $item3['BehaviourType_ID']  ; ?>"><?= $item3['Behaviour_Type'] . "---" . $item3['Behaviour_Description']; ?></option>
   <?php endforeach; ?>  </select>
 <input type="submit" name="button" value="Submit">
</form> -->
<p>
  <?php header("Location: contentviewbehaviours.php");
exit();
?>
  <a href="home.html">Home</a></p>
  </body>
  </html>