#pragma strict
public var prov : ExtractProvenance = null;
public var hp : Health;
public var enemyType : String;

function Awake()
{
	// Load provenance pointers
	hp = GetComponent(Health);
	var provObj : GameObject = GameObject.Find("Provenance");
	prov = GetComponent(ExtractProvenance); 
	 
	if(prov == null)
	{
		prov = GetComponentInParent(ExtractProvenance); 
	}
	
	prov.influenceContainer = provObj.GetComponent(InfluenceController); 
	prov.provenance = provObj.GetComponent(ProvenanceController); 
	
	Prov_Enemy();
	
}

//==========================================================
// Configurable
//==========================================================
// Enemy Attributes
public function Prov_GetEnemyAttributes()
{ 
	prov.AddAttribute("Health", hp.health.ToString());
}


//==========================================================
// Enemy
//==========================================================
// <INTERFACE> Enemy Agent
public function Prov_Enemy()
{
	Prov_GetEnemyAttributes();
	prov.NewAgentVertex(enemyType);
	Prov_Idle();
}

// <INTERFACE> Enemy Idle action
public function Prov_Idle()
{
	Prov_GetEnemyAttributes();
	prov.NewActivityVertex("Idle", this.gameObject);
	prov.HasInfluence("Enemy");
}

// <INTERFACE> Enemy Spot action
public function Prov_OnSpot()
{
	Prov_GetEnemyAttributes();
	prov.NewActivityVertex("Spotted", this.gameObject);
	prov.GenerateInfluenceC("Player", this.GetInstanceID().ToString(), "Spotted", "1", 1);
	prov.HasInfluence("Enemy");
	return this.GetInstanceID().ToString();
}

// <INTERFACE> Enemy Lost Track action
public function Prov_LostTrack()
{
	Prov_GetEnemyAttributes();
	prov.NewActivityVertex("LostTrack", this.gameObject);
	prov.GenerateInfluenceC("Player", this.GetInstanceID().ToString(), "Spotted", "-1", 1);
	prov.HasInfluence("Enemy");
	return this.GetInstanceID().ToString();
}

// <INTERFACE> Enemy Attack action
public function Prov_Attack(damageAmount : float)
{
	Prov_GetEnemyAttributes();
	prov.NewActivityVertex("Attacking", this.gameObject);
	prov.HasInfluence("Enemy");
	prov.GenerateInfluenceCE("PlayerDamage", this.GetInstanceID().ToString(), "Health (Player)", (-damageAmount).ToString(), 1, Time.time + 5);
	return this.GetInstanceID().ToString();
}

// <INTERFACE> Player Interact action
public function Prov_Regenerate(regValue : float)
{
	prov.GenerateInfluenceC("Player", this.GetInstanceID().ToString(), "Health (Player)", regValue.ToString(), 1);
}

// <INTERFACE> Enemy Death action
public function Prov_Death(scoreValue : float)
{
	Prov_GetEnemyAttributes();
	prov.NewActivityVertex("Dead", this.gameObject);
	prov.HasInfluence("Enemy");
	
	if(scoreValue != 0)
		prov.GenerateInfluenceC("Player", this.GetInstanceID().ToString(), "Score", scoreValue.ToString(), 1);
}

// <INTERFACE> Enemy Death action
public function Prov_Suicide()
{
	Prov_GetEnemyAttributes();
	prov.NewActivityVertex("Suicided", this.gameObject);
	prov.HasInfluence("Enemy");
	prov.GenerateInfluenceC("Player", this.GetInstanceID().ToString(), "Score Missed", "0", 1);
}

// <INTERFACE> Enemy Escaped (the scene) action
public function Prov_Escaped()
{
	Prov_GetEnemyAttributes();
	prov.NewActivityVertex("Escaped", this.gameObject);
	prov.HasInfluence("Enemy");
	prov.GenerateInfluenceC("Player", this.GetInstanceID().ToString(), "Score Missed", "0", 1);
}

// <INTERFACE> Enemy took damage
function Prov_TakeDamage(enemy : GameObject, damageAmount : float)
{
	Prov_generateTakeDamage(damageAmount);
	Prov_Hurt(this.GetInstanceID().ToString());
}

// Influence from taking damage
function Prov_generateTakeDamage(damageAmount : float)
{
	var heroObj : GameObject = GameObject.FindGameObjectWithTag("Player");
	var player : PlayerProv = heroObj.GetComponent(PlayerProv);
	player.Prov_Attack();
	player.prov.GenerateInfluenceC("Enemy", this.GetInstanceID().ToString(), "Health (Enemy)", (-damageAmount).ToString(), 1);
}

// Enemy took damage
public function Prov_Hurt(infID : String)
{
	Prov_GetEnemyAttributes();
	prov.NewActivityVertex("Taking Hit", this.gameObject);
	prov.HasInfluence_ID(infID);
}
	