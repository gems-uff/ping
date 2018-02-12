#pragma strict
public var prov : ExtractProvenance = null;
public var hp : Health;

function Awake()
{
	// Load provenance pointers
	hp = GetComponent(Health);
	prov = GetComponent(ExtractProvenance); 
	if(prov == null)	{
		prov = GetComponentInParent(ExtractProvenance); 
	}
	Prov_Player();
	
	InvokeRepeating("Prov_Walk", 2, 2);
}

//==========================================================
// Configurable
//==========================================================

// Player attributes
public function Prov_GetPlayerAttributes()
{
	prov.AddAttribute("Health", hp.health.ToString());
}

//==========================================================
// Player
//==========================================================

// Player agent
public function Prov_Player()
{
	Prov_GetPlayerAttributes();
	prov.NewAgentVertex("Player");
}

// Player Walk action
public function Prov_Walk()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("Walking");
	prov.HasInfluence("Player");
}

// Player Jump action
public function Prov_Jump()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("Jump");
	prov.HasInfluence("Player");
}

// Player Interact action
public function Prov_Interact()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("Interacted");
	prov.HasInfluence("Player");
}

// Player Interact action
public function Prov_Regenerate(regValue : float)
{
	prov.GenerateInfluenceC("Player", this.GetInstanceID().ToString(), "Health (Player)", regValue.ToString(), 1);
}

// Player attack action
public function Prov_Attack()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("Shooting");
	prov.HasInfluence("Player");
}

// Player took damage
function Prov_TakeDamage(enemy : GameObject, damageAmount : float)
{
	var enemyProv : EnemyProv = enemy.GetComponent(EnemyProv); 
	
	if(enemyProv == null)
	{
		enemyProv = enemy.GetComponentInParent(EnemyProv); 
	}
	
	var infID : String = enemyProv.Prov_Attack(damageAmount);
	this.Prov_TakeDamage(infID);
}

// Player took damage
public function Prov_TakeDamage(infID : String)
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("Being Hit");
	// Check Influence
	prov.HasInfluence_ID(infID);
}

// Player took damage
public function Prov_TakeDamage()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("Being Hit");
	// Check Influence
	prov.HasInfluence("PlayerDamage");
}

//Player Death action
public function Prov_Death()
{	
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("Dead");
	prov.GenerateInfluenceC("Player", this.GetInstanceID().ToString(), "Respawned", "-1", 1);
	//Prov_Export();
}

// Player Death action
public function Prov_Respawn()
{	
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("Respawn");
	prov.HasInfluence("Player");
	//Prov_Export();
}